using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface para repositório de lembretes
/// </summary>
public interface IReminderRepository : IRepository<Reminder>
{
    /// <summary>
    /// Obtém lembretes por contato
    /// </summary>
    Task<IEnumerable<Reminder>> GetByContactIdAsync(int contactId);

    /// <summary>
    /// Obtém lembretes por usuário
    /// </summary>
    Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Obtém lembretes de hoje por usuário
    /// </summary>
    Task<IEnumerable<Reminder>> GetTodayByUserIdAsync(int userId);

    /// <summary>
    /// Obtém lembretes atrasados por usuário
    /// </summary>
    Task<IEnumerable<Reminder>> GetOverdueByUserIdAsync(int userId);

    /// <summary>
    /// Obtém contagem de lembretes pendentes por contato
    /// </summary>
    Task<int> GetPendingCountByContactIdAsync(int contactId);

    /// <summary>
    /// Obtém lembretes com relacionamentos
    /// </summary>
    Task<Reminder?> GetByIdWithRelationsAsync(int id);
}

