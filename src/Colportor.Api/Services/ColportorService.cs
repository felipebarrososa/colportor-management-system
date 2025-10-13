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

            // Verificar permissões de região
            if (userRole == "Leader")
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.RegionId != createDto.RegionId)
                {
                    return ApiResponse<ColportorDto>.ErrorResponse("Você só pode criar colportores em sua região");
                }
            }

            var colportor = new Colportor
            {
                FullName = createDto.FullName,
                CPF = createDto.CPF,
                Gender = createDto.Gender,
                BirthDate = createDto.BirthDate,
                RegionId = createDto.RegionId,
                LeaderId = createDto.LeaderId ?? (userRole == "Leader" ? userId : null),
                City = createDto.City,
                PhotoUrl = createDto.PhotoUrl,
                LastVisitDate = createDto.LastVisitDate
            };

            var createdColportor = await _colportorRepository.AddAsync(colportor);
            var colportorDto = _mapper.Map<ColportorDto>(createdColportor);

            _logger.LogInformation("Colportor {ColportorId} criado com sucesso", createdColportor.Id);
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
                ColportorsByStatus = new Dictionary<string, int> // Implementar se necessário
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de colportores");
            throw;
        }
    }
}
