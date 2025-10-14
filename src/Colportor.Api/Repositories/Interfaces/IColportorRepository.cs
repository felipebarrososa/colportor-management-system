using Colportor.Api.Models;
using Colportor.Api.Data;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface para repositório de colportores
/// </summary>
public interface IColportorRepository : IRepository<Models.Colportor>
{
    /// <summary>
    /// Obtém colportor com relacionamentos
    /// </summary>
    Task<Models.Colportor?> GetByIdWithRelationsAsync(int id);

    /// <summary>
    /// Obtém colportores por região
    /// </summary>
    Task<IEnumerable<Models.Colportor>> GetByRegionAsync(int regionId);

    /// <summary>
    /// Obtém colportores por líder
    /// </summary>
    Task<IEnumerable<Models.Colportor>> GetByLeaderAsync(int leaderId);

    /// <summary>
    /// Obtém colportores por CPF
    /// </summary>
    Task<Models.Colportor?> GetByCPFAsync(string cpf);

    /// <summary>
    /// Obtém estatísticas de colportores
    /// </summary>
    Task<Models.ColportorStats> GetColportorStatsAsync();

    /// <summary>
    /// Obtém colportores com paginação e filtros
    /// </summary>
    Task<(IEnumerable<Models.Colportor> Items, int TotalCount)> GetPagedWithFiltersAsync(
        int page, 
        int pageSize, 
        int? regionId = null,
        int? leaderId = null,
        string? gender = null,
        string? searchTerm = null);

    /// <summary>
    /// Obtém inscrições PAC com filtros
    /// </summary>
    Task<IEnumerable<Models.PacEnrollment>> GetPacEnrollmentsAsync(DateTime? from = null, DateTime? to = null, int? leaderId = null);

    /// <summary>
    /// Adiciona uma nova inscrição PAC
    /// </summary>
    Task<Models.PacEnrollment> AddPacEnrollmentAsync(Models.PacEnrollment enrollment);

    /// <summary>
    /// Aprova uma inscrição PAC
    /// </summary>
    Task<bool> ApprovePacEnrollmentAsync(int enrollmentId);

    /// <summary>
    /// Rejeita uma inscrição PAC
    /// </summary>
    Task<bool> RejectPacEnrollmentAsync(int enrollmentId);

    /// <summary>
    /// Obtém o contexto do banco de dados
    /// </summary>
    AppDbContext GetContext();
}

