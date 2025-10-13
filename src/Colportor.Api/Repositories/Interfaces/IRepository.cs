using System.Linq.Expressions;

namespace Colportor.Api.Repositories;

/// <summary>
/// Interface base para repositórios
/// </summary>
/// <typeparam name="T">Tipo da entidade</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Obtém entidade por ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Obtém todas as entidades
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Obtém entidades com filtro
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Obtém entidade com filtro
    /// </summary>
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adiciona nova entidade
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Atualiza entidade
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Remove entidade
    /// </summary>
    Task RemoveAsync(T entity);

    /// <summary>
    /// Remove entidade por ID
    /// </summary>
    Task RemoveByIdAsync(int id);

    /// <summary>
    /// Verifica se existe entidade com filtro
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Conta entidades com filtro
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Obtém entidades paginadas
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true);
}
