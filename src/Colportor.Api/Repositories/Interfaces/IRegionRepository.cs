using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface para repositório de regiões
/// </summary>
public interface IRegionRepository : IRepository<Region>
{
    /// <summary>
    /// Obtém região com relacionamentos
    /// </summary>
    Task<Region?> GetByIdWithRelationsAsync(int id);

    /// <summary>
    /// Obtém região por nome e país
    /// </summary>
    Task<Region?> GetByNameAndCountryAsync(string name, int countryId);

    /// <summary>
    /// Obtém regiões por país
    /// </summary>
    Task<IEnumerable<Region>> GetByCountryAsync(int countryId);

    /// <summary>
    /// Obtém estatísticas da região
    /// </summary>
    Task<RegionStats> GetRegionStatsAsync(int regionId);
}

/// <summary>
/// Estatísticas da região
/// </summary>
public class RegionStats
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
}
