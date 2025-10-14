using Colportor.Api.DTOs;
using Colportor.Api.Models;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviços de regiões
/// </summary>
public interface IRegionService
{
    /// <summary>
    /// Lista regiões baseado no usuário
    /// </summary>
    Task<List<RegionDto>> GetRegionsAsync(int userId, string userRole);

    /// <summary>
    /// Obtém região por ID
    /// </summary>
    Task<RegionDto?> GetRegionByIdAsync(int id);

    /// <summary>
    /// Cria nova região
    /// </summary>
    Task<ApiResponse<RegionDto>> CreateRegionAsync(RegionCreateDto createDto);

    /// <summary>
    /// Atualiza região
    /// </summary>
    Task<ApiResponse<RegionDto>> UpdateRegionAsync(int id, RegionCreateDto updateDto);

    /// <summary>
    /// Deleta região
    /// </summary>
    Task<ApiResponse> DeleteRegionAsync(int id);

    /// <summary>
    /// Obtém estatísticas de uma região
    /// </summary>
    Task<RegionStatsDto?> GetRegionStatsAsync(int id);

    /// <summary>
    /// Lista todas as regiões (para admin)
    /// </summary>
    Task<IEnumerable<RegionDto>> GetAllAsync();

    /// <summary>
    /// Lista todos os países
    /// </summary>
    Task<IEnumerable<CountryDto>> GetCountriesAsync();

    /// <summary>
    /// Cria um novo país
    /// </summary>
    Task<ApiResponse<CountryDto>> CreateCountryAsync(CountryCreateDto createDto);

    /// <summary>
    /// Lista regiões por país
    /// </summary>
    Task<IEnumerable<RegionDto>> GetRegionsByCountryAsync(int countryId);
}

/// <summary>
/// DTO para dados da região
/// </summary>
public class RegionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public int ColportorsCount { get; set; }
    public int LeadersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para estatísticas da região
/// </summary>
public class RegionStatsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalColportors { get; set; }
    public int ActiveColportors { get; set; }
    public int MaleColportors { get; set; }
    public int FemaleColportors { get; set; }
    public int TotalLeaders { get; set; }
    public int TotalVisits { get; set; }
    public int TotalPacEnrollments { get; set; }
    public DateTime LastActivity { get; set; }
    public Dictionary<string, int> ColportorsByGender { get; set; } = new();
    public Dictionary<string, int> ActivityByMonth { get; set; } = new();
}

/// <summary>
/// DTO para dados do país
/// </summary>
public class CountryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // Mapeado de Iso2
    public int RegionsCount { get; set; }
}
