using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface para repositório de colportores
/// </summary>
public interface IColportorRepository : IRepository<Colportor>
{
    /// <summary>
    /// Obtém colportor com relacionamentos
    /// </summary>
    Task<Colportor?> GetByIdWithRelationsAsync(int id);

    /// <summary>
    /// Obtém colportores por região
    /// </summary>
    Task<IEnumerable<Colportor>> GetByRegionAsync(int regionId);

    /// <summary>
    /// Obtém colportores por líder
    /// </summary>
    Task<IEnumerable<Colportor>> GetByLeaderAsync(int leaderId);

    /// <summary>
    /// Obtém colportores por CPF
    /// </summary>
    Task<Colportor?> GetByCPFAsync(string cpf);

    /// <summary>
    /// Obtém estatísticas de colportores
    /// </summary>
    Task<ColportorStats> GetColportorStatsAsync();

    /// <summary>
    /// Obtém colportores com paginação e filtros
    /// </summary>
    Task<(IEnumerable<Colportor> Items, int TotalCount)> GetPagedWithFiltersAsync(
        int page, 
        int pageSize, 
        int? regionId = null,
        int? leaderId = null,
        string? gender = null,
        string? searchTerm = null);
}

/// <summary>
/// Estatísticas de colportores
/// </summary>
public class ColportorStats
{
    public int TotalColportors { get; set; }
    public int ActiveColportors { get; set; }
    public int MaleColportors { get; set; }
    public int FemaleColportors { get; set; }
    public Dictionary<string, int> ColportorsByRegion { get; set; } = new();
    public Dictionary<string, int> ColportorsByGender { get; set; } = new();
}
