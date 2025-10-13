using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface para repositório de usuários
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Obtém usuário por email
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Obtém usuário com relacionamentos
    /// </summary>
    Task<User?> GetByIdWithRelationsAsync(int id);

    /// <summary>
    /// Obtém usuários por role
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(string role);

    /// <summary>
    /// Verifica se email existe
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Obtém usuários por região (para líderes)
    /// </summary>
    Task<IEnumerable<User>> GetByRegionAsync(int regionId);
}
