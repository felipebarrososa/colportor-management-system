using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Colportor.Api.Services
{
    public class MissionContactService : IMissionContactService
    {
        private readonly IMissionContactRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly IRegionRepository _regionRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<MissionContactService> _logger;

        public MissionContactService(
            IMissionContactRepository repo,
            IUserRepository userRepo,
            IRegionRepository regionRepo,
            IMapper mapper,
            ILogger<MissionContactService> logger)
        {
            _repo = repo;
            _userRepo = userRepo;
            _regionRepo = regionRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<MissionContactDto>> CreateAsync(MissionContactCreateDto dto, int currentUserId, string currentUserRole)
        {
            try
            {
                var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                if (currentUser == null) return ApiResponse<MissionContactDto>.ErrorResponse("Usuário não encontrado");

                // Regras de líder: só pode criar na própria região
                if (currentUserRole == "Leader")
                {
                    if (currentUser.RegionId != dto.RegionId)
                        return ApiResponse<MissionContactDto>.ErrorResponse("Líder só pode criar contatos na sua região");
                }

                // Normalização
                var phone = NormalizePhone(dto.Phone);
                var email = NormalizeEmail(dto.Email);

                var entity = new MissionContact
                {
                    FullName = dto.FullName,
                    Gender = dto.Gender,
                    BirthDate = NormalizeInUtc(dto.BirthDate),
                    MaritalStatus = dto.MaritalStatus,
                    Nationality = dto.Nationality,
                    City = dto.City,
                    State = dto.State,
                    Phone = phone,
                    Email = email,
                    Profession = dto.Profession,
                    SpeaksOtherLanguages = dto.SpeaksOtherLanguages,
                    OtherLanguages = dto.OtherLanguages,
                    FluencyLevel = dto.FluencyLevel,
                    Church = dto.Church,
                    ConversionTime = dto.ConversionTime,
                    MissionsDedicationPlan = dto.MissionsDedicationPlan,
                    HasPassport = dto.HasPassport,
                    AvailableDate = NormalizeInUtc(dto.AvailableDate),

                    RegionId = dto.RegionId,
                    LeaderId = dto.LeaderId ?? (currentUserRole == "Leader" ? currentUserId : null),
                    CreatedByUserId = currentUserId,
                    CreatedByColportorId = currentUser.ColportorId,
                    Status = "Novo",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _repo.AddAsync(entity);
                var result = _mapper.Map<MissionContactDto>(created);
                return ApiResponse<MissionContactDto>.SuccessResponse(result, "Contato criado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar contato missionário");
                return ApiResponse<MissionContactDto>.ErrorResponse("Erro interno do servidor");
            }
        }

        public async Task<ApiResponse<MissionContactDto>> UpdateAsync(int id, MissionContactUpdateDto dto, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity == null) return ApiResponse<MissionContactDto>.ErrorResponse("Contato não encontrado");

                // Permissões: Admin total; Leader somente na sua região; Colportor sem permissão
                if (currentUserRole == "Leader")
                {
                    var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                    if (currentUser?.RegionId != entity.RegionId)
                        return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão para editar este contato");
                }
                else if (currentUserRole == "Colportor")
                {
                    return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão para editar");
                }

                if (dto.FullName != null) entity.FullName = dto.FullName;
                if (dto.Gender != null) entity.Gender = dto.Gender;
                if (dto.BirthDate.HasValue) entity.BirthDate = NormalizeInUtc(dto.BirthDate);
                if (dto.MaritalStatus != null) entity.MaritalStatus = dto.MaritalStatus;
                if (dto.Nationality != null) entity.Nationality = dto.Nationality;
                if (dto.City != null) entity.City = dto.City;
                if (dto.State != null) entity.State = dto.State;
                if (dto.Phone != null) entity.Phone = NormalizePhone(dto.Phone);
                if (dto.Email != null) entity.Email = NormalizeEmail(dto.Email);
                if (dto.Profession != null) entity.Profession = dto.Profession;
                if (dto.SpeaksOtherLanguages.HasValue) entity.SpeaksOtherLanguages = dto.SpeaksOtherLanguages.Value;
                if (dto.OtherLanguages != null) entity.OtherLanguages = dto.OtherLanguages;
                if (dto.FluencyLevel != null) entity.FluencyLevel = dto.FluencyLevel;
                if (dto.Church != null) entity.Church = dto.Church;
                if (dto.ConversionTime != null) entity.ConversionTime = dto.ConversionTime;
                if (dto.MissionsDedicationPlan != null) entity.MissionsDedicationPlan = dto.MissionsDedicationPlan;
                if (dto.HasPassport.HasValue) entity.HasPassport = dto.HasPassport.Value;
                if (dto.AvailableDate.HasValue) entity.AvailableDate = NormalizeInUtc(dto.AvailableDate);
                if (dto.Notes != null) entity.Notes = dto.Notes;

                // Region/Leader updates (Admin pode; Líder apenas se a região continuar igual à dele)
                if (dto.RegionId.HasValue)
                {
                    if (currentUserRole == "Admin")
                    {
                        entity.RegionId = dto.RegionId.Value;
                    }
                    else
                    {
                        var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                        if (currentUser?.RegionId != dto.RegionId.Value)
                            return ApiResponse<MissionContactDto>.ErrorResponse("Líder só pode manter a própria região");
                        entity.RegionId = dto.RegionId.Value;
                    }
                }
                if (dto.LeaderId.HasValue)
                {
                    if (currentUserRole == "Admin")
                    {
                        entity.LeaderId = dto.LeaderId.Value;
                    }
                    else if (currentUserRole == "Leader")
                    {
                        // Líder só consegue se atribuir
                        if (dto.LeaderId.Value != currentUserId)
                            return ApiResponse<MissionContactDto>.ErrorResponse("Líder só pode se atribuir");
                        entity.LeaderId = currentUserId;
                    }
                }

                entity.UpdatedAt = DateTime.UtcNow;
                var updated = await _repo.UpdateAsync(entity);
                return ApiResponse<MissionContactDto>.SuccessResponse(_mapper.Map<MissionContactDto>(updated), "Contato atualizado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar contato missionário {Id}", id);
                return ApiResponse<MissionContactDto>.ErrorResponse("Erro interno do servidor");
            }
        }

        public async Task<ApiResponse<MissionContactDto>> UpdateStatusAsync(int id, MissionContactStatusUpdateDto dto, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity == null) return ApiResponse<MissionContactDto>.ErrorResponse("Contato não encontrado");

                if (currentUserRole == "Leader")
                {
                    var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                    if (currentUser?.RegionId != entity.RegionId)
                        return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão para alterar status deste contato");
                }
                else if (currentUserRole == "Colportor")
                {
                    return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão para alterar status");
                }

                entity.Status = dto.Status;
                entity.LastContactedAt = NormalizeInUtc(dto.LastContactedAt);
                entity.NextFollowUpAt = NormalizeInUtc(dto.NextFollowUpAt);
                if (dto.Notes != null) entity.Notes = dto.Notes;
                entity.UpdatedAt = DateTime.UtcNow;

                var updated = await _repo.UpdateAsync(entity);
                return ApiResponse<MissionContactDto>.SuccessResponse(_mapper.Map<MissionContactDto>(updated), "Status atualizado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do contato missionário {Id}", id);
                return ApiResponse<MissionContactDto>.ErrorResponse("Erro interno do servidor");
            }
        }

        public async Task<ApiResponse<MissionContactDto>> AssignLeaderAsync(int id, int leaderId, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity == null) return ApiResponse<MissionContactDto>.ErrorResponse("Contato não encontrado");

                if (currentUserRole == "Admin")
                {
                    entity.LeaderId = leaderId;
                }
                else if (currentUserRole == "Leader")
                {
                    // Líder só pode se atribuir e precisa ser da mesma região
                    var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                    if (currentUser?.RegionId != entity.RegionId)
                        return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão para atribuir este contato");
                    if (leaderId != currentUserId)
                        return ApiResponse<MissionContactDto>.ErrorResponse("Líder só pode se atribuir");
                    entity.LeaderId = currentUserId;
                }
                else
                {
                    return ApiResponse<MissionContactDto>.ErrorResponse("Sem permissão");
                }

                entity.UpdatedAt = DateTime.UtcNow;
                var updated = await _repo.UpdateAsync(entity);
                return ApiResponse<MissionContactDto>.SuccessResponse(_mapper.Map<MissionContactDto>(updated), "Atribuição atualizada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atribuir líder ao contato missionário {Id}", id);
                return ApiResponse<MissionContactDto>.ErrorResponse("Erro interno do servidor");
            }
        }

        public async Task<ApiResponse> DeleteAsync(int id, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity == null) return ApiResponse.ErrorResponse("Contato não encontrado");

                if (currentUserRole == "Admin")
                {
                    await _repo.RemoveAsync(entity);
                    return ApiResponse.SuccessResponse("Contato removido");
                }

                return ApiResponse.ErrorResponse("Sem permissão");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover contato missionário {Id}", id);
                return ApiResponse.ErrorResponse("Erro interno do servidor");
            }
        }

        public async Task<PagedResult<MissionContactDto>> GetPagedAsync(MissionContactFilterDto filter, int currentUserId, string currentUserRole)
        {
            try
            {
                int? regionId = filter.RegionId;
                int? leaderId = filter.LeaderId;

                if (currentUserRole == "Leader")
                {
                    var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                    regionId = currentUser?.RegionId;
                }
                else if (currentUserRole == "Colportor")
                {
                    // Colportor: listar apenas os próprios (via CreatedByUserId)
                    // Como o repositório não filtra por CreatedByUserId, filtraremos após o fetch paginado por região (opcional)
                }

                var (items, total) = await _repo.GetPagedAsync(
                    filter.Page,
                    filter.PageSize,
                    regionId,
                    leaderId,
                    filter.Status,
                    filter.SearchTerm,
                    filter.CreatedFrom,
                    filter.CreatedTo,
                    filter.AvailableFrom,
                    filter.AvailableTo);

                // Restringir para colportor: só os próprios
                if (currentUserRole == "Colportor")
                {
                    items = items.Where(i => i.CreatedByUserId == currentUserId).ToList();
                    total = items.Count();
                }

                var dtos = items.Select(i => _mapper.Map<MissionContactDto>(i)).ToList();
                return new PagedResult<MissionContactDto>
                {
                    Items = dtos,
                    TotalCount = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar contatos missionários");
                throw;
            }
        }

        public async Task<MissionContactDto?> GetByIdAsync(int id, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                if (entity == null) return null;

                if (currentUserRole == "Leader")
                {
                    var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                    if (currentUser?.RegionId != entity.RegionId) return null;
                }
                else if (currentUserRole == "Colportor")
                {
                    if (entity.CreatedByUserId != currentUserId) return null;
                }

                return _mapper.Map<MissionContactDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter contato missionário {Id}", id);
                throw;
            }
        }

        private static string? NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email) ? email : email.Trim().ToLowerInvariant();
        }

        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits;
        }

        private static DateTime? NormalizeInUtc(DateTime? date)
        {
            if (!date.HasValue) return null;
            if (date.Value.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
            return date.Value.ToUniversalTime();
        }
    }
}



