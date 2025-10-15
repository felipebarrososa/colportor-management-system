using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories;
using Colportor.Api.Services;

namespace Colportor.Api.Services;

/// <summary>
/// Implementação do serviço de colportores
/// </summary>
public class ColportorService : IColportorService
{
    private readonly IColportorRepository _colportorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ColportorService> _logger;

    public ColportorService(
        IColportorRepository colportorRepository,
        IUserRepository userRepository,
        IRegionRepository regionRepository,
        IMapper mapper,
        ILogger<ColportorService> logger)
    {
        _colportorRepository = colportorRepository;
        _userRepository = userRepository;
        _regionRepository = regionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ColportorDto>> GetColportorsAsync(int userId, string userRole, int page = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Listando colportores para usuário {UserId} com role {Role}", userId, userRole);

            int? regionId = null;
            int? leaderId = null;

            // Filtrar por região se for líder
            if (userRole == "Leader")
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.RegionId.HasValue == true)
                {
                    regionId = user.RegionId.Value;
                    leaderId = userId;
                }
            }

            var (colportors, totalCount) = await _colportorRepository.GetPagedWithFiltersAsync(
                page, pageSize, regionId, leaderId);

            var colportorDtos = colportors.Select(c => _mapper.Map<ColportorDto>(c)).ToList();

            var result = new PagedResult<ColportorDto>
            {
                Items = colportorDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation("Retornando {Count} colportores de {Total} total", colportorDtos.Count, totalCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar colportores");
            throw;
        }
    }

    public async Task<ColportorDto?> GetColportorByIdAsync(int id, int userId, string userRole)
    {
        try
        {
            _logger.LogInformation("Buscando colportor {ColportorId} para usuário {UserId}", id, userId);

            var colportor = await _colportorRepository.GetByIdWithRelationsAsync(id);
            if (colportor == null)
            {
                return null;
            }

            // Verificar permissões
            if (userRole == "Leader" && colportor.LeaderId != userId)
            {
                _logger.LogWarning("Usuário {UserId} tentou acessar colportor {ColportorId} sem permissão", userId, id);
                return null;
            }

            return _mapper.Map<ColportorDto>(colportor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar colportor {ColportorId}", id);
            throw;
        }
    }

    public async Task<ApiResponse<ColportorDto>> CreateColportorAsync(CreateColportorDto createDto, int userId, string userRole)
    {
        try
        {
            _logger.LogInformation("Criando colportor {FullName} para usuário {UserId}", createDto.FullName, userId);

            // Verificar se CPF já existe
            if (await _colportorRepository.GetByCPFAsync(createDto.CPF) != null)
            {
                return ApiResponse<ColportorDto>.ErrorResponse("CPF já está cadastrado");
            }

            // Verificar se email já existe
            if (await _userRepository.EmailExistsAsync(createDto.Email))
            {
                return ApiResponse<ColportorDto>.ErrorResponse("Email já está cadastrado");
            }

            // Verificar permissões de região
            if (userRole == "Leader")
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.RegionId != createDto.RegionId)
                {
                    return ApiResponse<ColportorDto>.ErrorResponse("Você só pode criar colportores em sua região");
                }
            }

            // Criar o usuário primeiro
            var user = new Models.User
            {
                Email = createDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
                Role = "Colportor",
                FullName = createDto.FullName,
                CPF = createDto.CPF,
                City = createDto.City,
                RegionId = createDto.RegionId
            };

            var createdUser = await _userRepository.AddAsync(user);

            // Criar o colportor
            var colportor = new Models.Colportor
            {
                FullName = createDto.FullName,
                CPF = createDto.CPF,
                Gender = createDto.Gender,
                BirthDate = createDto.BirthDate.HasValue ? 
                    DateTime.SpecifyKind(createDto.BirthDate.Value, DateTimeKind.Local).ToUniversalTime() : 
                    null,
                RegionId = createDto.RegionId,
                LeaderId = createDto.LeaderId ?? (userRole == "Leader" ? userId : null),
                City = createDto.City,
                PhotoUrl = createDto.PhotoUrl,
                LastVisitDate = createDto.LastVisitDate.HasValue ? 
                    DateTime.SpecifyKind(createDto.LastVisitDate.Value, DateTimeKind.Local).ToUniversalTime() : 
                    null
            };

            var createdColportor = await _colportorRepository.AddAsync(colportor);

            // Associar o usuário ao colportor
            createdUser.ColportorId = createdColportor.Id;
            await _userRepository.UpdateAsync(createdUser);

            var colportorDto = _mapper.Map<ColportorDto>(createdColportor);

            _logger.LogInformation("Colportor {ColportorId} e User {UserId} criados com sucesso", createdColportor.Id, createdUser.Id);
            return ApiResponse<ColportorDto>.SuccessResponse(colportorDto, "Colportor criado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar colportor");
            return ApiResponse<ColportorDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse<ColportorDto>> UpdateColportorAsync(int id, UpdateColportorDto updateDto, int userId, string userRole)
    {
        try
        {
            _logger.LogInformation("Atualizando colportor {ColportorId} para usuário {UserId}", id, userId);

            var colportor = await _colportorRepository.GetByIdAsync(id);
            if (colportor == null)
            {
                return ApiResponse<ColportorDto>.ErrorResponse("Colportor não encontrado");
            }

            // Verificar permissões
            if (userRole == "Leader" && colportor.LeaderId != userId)
            {
                return ApiResponse<ColportorDto>.ErrorResponse("Você não tem permissão para editar este colportor");
            }

            // Atualizar campos
            colportor.FullName = updateDto.FullName;
            colportor.Gender = updateDto.Gender;
            colportor.BirthDate = updateDto.BirthDate;
            colportor.City = updateDto.City;
            colportor.PhotoUrl = updateDto.PhotoUrl;
            colportor.LastVisitDate = updateDto.LastVisitDate;

            var updatedColportor = await _colportorRepository.UpdateAsync(colportor);
            var colportorDto = _mapper.Map<ColportorDto>(updatedColportor);

            _logger.LogInformation("Colportor {ColportorId} atualizado com sucesso", id);
            return ApiResponse<ColportorDto>.SuccessResponse(colportorDto, "Colportor atualizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar colportor {ColportorId}", id);
            return ApiResponse<ColportorDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse> DeleteColportorAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deletando colportor {ColportorId}", id);

            var colportor = await _colportorRepository.GetByIdAsync(id);
            if (colportor == null)
            {
                return ApiResponse.ErrorResponse("Colportor não encontrado");
            }

            await _colportorRepository.RemoveAsync(colportor);

            _logger.LogInformation("Colportor {ColportorId} deletado com sucesso", id);
            return ApiResponse.SuccessResponse("Colportor deletado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar colportor {ColportorId}", id);
            return ApiResponse.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ColportorStatsDto> GetColportorStatsAsync()
    {
        try
        {
            _logger.LogInformation("Obtendo estatísticas de colportores");

            var stats = await _colportorRepository.GetColportorStatsAsync();
            
            return new ColportorStatsDto
            {
                TotalColportors = stats.TotalColportors,
                ActiveColportors = stats.ActiveColportors,
                MaleColportors = stats.MaleColportors,
                FemaleColportors = stats.FemaleColportors,
                ColportorsByRegion = stats.ColportorsByRegion,
                ColportorsByStatus = new Dictionary<string, int>() // Implementar se necessário
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de colportores");
            throw;
        }
    }

    public async Task<PagedResult<ColportorDto>> GetPagedAsync(int page = 1, int pageSize = 10, int? regionId = null, int? leaderId = null, string? gender = null, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Listando colportores com paginação - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var (items, totalCount) = await _colportorRepository.GetPagedWithFiltersAsync(page, pageSize, regionId, leaderId, gender, searchTerm);
            var colportorDtos = _mapper.Map<List<ColportorDto>>(items);

            return new PagedResult<ColportorDto>
            {
                Items = colportorDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar colportores com paginação");
            throw;
        }
    }

    public async Task<IEnumerable<PacEnrollmentDto>> GetPacEnrollmentsAsync(DateTime? from = null, DateTime? to = null, int? leaderId = null)
    {
        try
        {
            _logger.LogInformation("Listando inscrições PAC - From: {From}, To: {To}, LeaderId: {LeaderId}", from, to, leaderId);

            var enrollments = await _colportorRepository.GetPacEnrollmentsAsync(from, to, leaderId);
            return _mapper.Map<List<PacEnrollmentDto>>(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar inscrições PAC");
            throw;
        }
    }

    public async Task<ApiResponse<bool>> CreatePacEnrollmentAsync(int leaderId, List<int> colportorIds, DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Criando inscrição PAC para líder {LeaderId} com {ColportorCount} colportores", leaderId, colportorIds.Count);
            _logger.LogInformation("Datas recebidas - StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);

            foreach (var colportorId in colportorIds)
            {
                var enrollment = new Models.PacEnrollment
                {
                    ColportorId = colportorId,
                    LeaderId = leaderId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                await _colportorRepository.AddPacEnrollmentAsync(enrollment);
            }

            _logger.LogInformation("Inscrição PAC criada com sucesso para líder {LeaderId}", leaderId);
            return ApiResponse<bool>.SuccessResponse(true, "Inscrição PAC criada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar inscrição PAC para líder {LeaderId}", leaderId);
                     return ApiResponse<bool>.ErrorResponse("Erro interno ao criar inscrição PAC");
                 }
             }

             public async Task<ApiResponse<bool>> ApprovePacEnrollmentAsync(int enrollmentId)
             {
                 try
                 {
                     _logger.LogInformation("Aprovando inscrição PAC {EnrollmentId}", enrollmentId);

                     var success = await _colportorRepository.ApprovePacEnrollmentAsync(enrollmentId);

                     if (!success)
                     {
                         return ApiResponse<bool>.ErrorResponse("Inscrição PAC não encontrada ou já foi processada");
                     }

                     _logger.LogInformation("Inscrição PAC {EnrollmentId} aprovada com sucesso", enrollmentId);
                     return ApiResponse<bool>.SuccessResponse(true, "Inscrição PAC aprovada com sucesso");
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Erro ao aprovar inscrição PAC {EnrollmentId}", enrollmentId);
                     return ApiResponse<bool>.ErrorResponse("Erro interno ao aprovar inscrição PAC");
                 }
             }

             public async Task<ApiResponse<bool>> RejectPacEnrollmentAsync(int enrollmentId)
             {
                 try
                 {
                     _logger.LogInformation("Rejeitando inscrição PAC {EnrollmentId}", enrollmentId);

                     var success = await _colportorRepository.RejectPacEnrollmentAsync(enrollmentId);

                     if (!success)
                     {
                         return ApiResponse<bool>.ErrorResponse("Inscrição PAC não encontrada ou já foi processada");
                     }

                     _logger.LogInformation("Inscrição PAC {EnrollmentId} rejeitada com sucesso", enrollmentId);
                     return ApiResponse<bool>.SuccessResponse(true, "Inscrição PAC rejeitada");
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Erro ao rejeitar inscrição PAC {EnrollmentId}", enrollmentId);
                     return ApiResponse<bool>.ErrorResponse("Erro interno ao rejeitar inscrição PAC");
                 }
             }

         public async Task<ApiResponse<VisitDto>> CreateVisitAsync(int colportorId, DateTime date)
         {
             try
             {
                 var visit = new Models.Visit
                 {
                     ColportorId = colportorId,
                     Date = DateTime.SpecifyKind(date, DateTimeKind.Local).ToUniversalTime()
                 };

                 var createdVisit = await _colportorRepository.GetContext().Visits.AddAsync(visit);
                 await _colportorRepository.GetContext().SaveChangesAsync();

                 // Atualizar LastVisitDate do colportor
                 var colportor = await _colportorRepository.GetByIdAsync(colportorId);
                 if (colportor != null)
                 {
                     colportor.LastVisitDate = visit.Date;
                     await _colportorRepository.GetContext().SaveChangesAsync();
                 }

                 var visitDto = new VisitDto
                 {
                     Id = visit.Id,
                     Date = visit.Date
                 };

                 _logger.LogInformation("Visita criada com sucesso: ID {VisitId}", visit.Id);
                 return ApiResponse<VisitDto>.SuccessResponse(visitDto, "Visita registrada com sucesso");
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Erro ao criar visita para colportor {ColportorId}", colportorId);
                 return ApiResponse<VisitDto>.ErrorResponse("Erro interno ao criar visita");
             }
         }
     }
