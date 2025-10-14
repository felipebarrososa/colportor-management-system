using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories;
using Colportor.Api.Services;

namespace Colportor.Api.Services;

/// <summary>
/// Implementação do serviço de regiões
/// </summary>
public class RegionService : IRegionService
{
    private readonly IRegionRepository _regionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RegionService> _logger;

    public RegionService(
        IRegionRepository regionRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<RegionService> logger)
    {
        _regionRepository = regionRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<RegionDto>> GetRegionsAsync(int userId, string userRole)
    {
        try
        {
            _logger.LogInformation("Listando regiões para usuário {UserId} com role {Role}", userId, userRole);

            List<Region> regions;

            if (userRole == "Admin")
            {
                regions = (await _regionRepository.GetAllAsync()).ToList();
            }
            else if (userRole == "Leader")
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.RegionId.HasValue == true)
                {
                    var region = await _regionRepository.GetByIdAsync(user.RegionId.Value);
                    regions = region != null ? new List<Region> { region } : new List<Region>();
                }
                else
                {
                    regions = new List<Region>();
                }
            }
            else
            {
                regions = new List<Region>();
            }

            var regionDtos = regions.Select(r => _mapper.Map<RegionDto>(r)).ToList();

            _logger.LogInformation("Retornando {Count} regiões", regionDtos.Count);
            return regionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar regiões");
            throw;
        }
    }

    public async Task<RegionDto?> GetRegionByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Buscando região {RegionId}", id);

            var region = await _regionRepository.GetByIdWithRelationsAsync(id);
            if (region == null)
            {
                return null;
            }

            return _mapper.Map<RegionDto>(region);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar região {RegionId}", id);
            throw;
        }
    }

    public async Task<ApiResponse<RegionDto>> CreateRegionAsync(RegionCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("Criando região {RegionName}", createDto.Name);

            // Verificar se região já existe no país
            var existingRegion = await _regionRepository.GetByNameAndCountryAsync(createDto.Name, createDto.CountryId);
            if (existingRegion != null)
            {
                return ApiResponse<RegionDto>.ErrorResponse("Região já existe neste país");
            }

            var region = new Region
            {
                Name = createDto.Name,
                CountryId = createDto.CountryId
            };

            var createdRegion = await _regionRepository.AddAsync(region);
            var regionDto = _mapper.Map<RegionDto>(createdRegion);

            _logger.LogInformation("Região {RegionId} criada com sucesso", createdRegion.Id);
            return ApiResponse<RegionDto>.SuccessResponse(regionDto, "Região criada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar região");
            return ApiResponse<RegionDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse<RegionDto>> UpdateRegionAsync(int id, RegionCreateDto updateDto)
    {
        try
        {
            _logger.LogInformation("Atualizando região {RegionId}", id);

            var region = await _regionRepository.GetByIdAsync(id);
            if (region == null)
            {
                return ApiResponse<RegionDto>.ErrorResponse("Região não encontrada");
            }

            // Verificar se nome já existe em outro país
            var existingRegion = await _regionRepository.GetByNameAndCountryAsync(updateDto.Name, updateDto.CountryId);
            if (existingRegion != null && existingRegion.Id != id)
            {
                return ApiResponse<RegionDto>.ErrorResponse("Região já existe neste país");
            }

            region.Name = updateDto.Name;
            region.CountryId = updateDto.CountryId;

            var updatedRegion = await _regionRepository.UpdateAsync(region);
            var regionDto = _mapper.Map<RegionDto>(updatedRegion);

            _logger.LogInformation("Região {RegionId} atualizada com sucesso", id);
            return ApiResponse<RegionDto>.SuccessResponse(regionDto, "Região atualizada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar região {RegionId}", id);
            return ApiResponse<RegionDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse> DeleteRegionAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deletando região {RegionId}", id);

            var region = await _regionRepository.GetByIdAsync(id);
            if (region == null)
            {
                return ApiResponse.ErrorResponse("Região não encontrada");
            }

            // Verificar se há colportores na região
            var colportorsCount = await _regionRepository.CountAsync(r => r.Id == id);
            if (colportorsCount > 0)
            {
                return ApiResponse.ErrorResponse("Não é possível deletar região que possui colportores");
            }

            await _regionRepository.RemoveAsync(region);

            _logger.LogInformation("Região {RegionId} deletada com sucesso", id);
            return ApiResponse.SuccessResponse("Região deletada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar região {RegionId}", id);
            return ApiResponse.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<RegionStatsDto?> GetRegionStatsAsync(int id)
    {
        try
        {
            _logger.LogInformation("Obtendo estatísticas da região {RegionId}", id);

            var region = await _regionRepository.GetByIdAsync(id);
            if (region == null)
            {
                return null;
            }

            var stats = await _regionRepository.GetRegionStatsAsync(id);
            
            return new RegionStatsDto
            {
                Id = stats.Id,
                Name = stats.Name,
                TotalColportors = stats.TotalColportors,
                ActiveColportors = stats.ActiveColportors,
                MaleColportors = stats.MaleColportors,
                FemaleColportors = stats.FemaleColportors,
                TotalLeaders = stats.TotalLeaders,
                TotalVisits = stats.TotalVisits,
                TotalPacEnrollments = stats.TotalPacEnrollments,
                LastActivity = stats.LastActivity,
                ColportorsByGender = new Dictionary<string, int>
                {
                    ["Masculino"] = stats.MaleColportors,
                    ["Feminino"] = stats.FemaleColportors
                },
                ActivityByMonth = new Dictionary<string, int>() // Implementar se necessário
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas da região {RegionId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<RegionDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Listando todas as regiões");

            var regions = await _regionRepository.GetAllAsync();
            return _mapper.Map<List<RegionDto>>(regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar todas as regiões");
            throw;
        }
    }

    public async Task<IEnumerable<CountryDto>> GetCountriesAsync()
    {
        try
        {
            _logger.LogInformation("Listando todos os países");

            var countries = await _regionRepository.GetCountriesAsync();
            return _mapper.Map<List<CountryDto>>(countries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar países");
            throw;
        }
    }

    public async Task<ApiResponse<CountryDto>> CreateCountryAsync(CountryCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("Criando novo país: {CountryName}", createDto.Name);

            // Validação básica
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return ApiResponse<CountryDto>.ErrorResponse("Nome do país é obrigatório");
            }

            if (string.IsNullOrWhiteSpace(createDto.Code))
            {
                return ApiResponse<CountryDto>.ErrorResponse("Código do país é obrigatório");
            }

            // Verificar se já existe um país com o mesmo nome ou código
            var existingCountries = await _regionRepository.GetCountriesAsync();
            if (existingCountries.Any(c => c.Name.Equals(createDto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return ApiResponse<CountryDto>.ErrorResponse("Já existe um país com este nome");
            }

            if (existingCountries.Any(c => c.Iso2?.Equals(createDto.Code, StringComparison.OrdinalIgnoreCase) == true))
            {
                return ApiResponse<CountryDto>.ErrorResponse("Já existe um país com este código");
            }

            var result = await _regionRepository.CreateCountryAsync(createDto.Name, createDto.Code);
            var countryDto = _mapper.Map<CountryDto>(result);

            _logger.LogInformation("País criado com sucesso: {CountryId} - {CountryName}", result.Id, result.Name);
            return ApiResponse<CountryDto>.SuccessResponse(countryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar país: {CountryName}", createDto.Name);
            return ApiResponse<CountryDto>.ErrorResponse("Erro interno ao criar país");
        }
    }

    public async Task<IEnumerable<RegionDto>> GetRegionsByCountryAsync(int countryId)
    {
        try
        {
            _logger.LogInformation("Listando regiões por país: {CountryId}", countryId);

            var regions = await _regionRepository.GetByCountryAsync(countryId);
            return _mapper.Map<List<RegionDto>>(regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar regiões por país: {CountryId}", countryId);
            throw;
        }
    }
}
