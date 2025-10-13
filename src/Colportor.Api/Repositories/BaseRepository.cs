using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Colportor.Api.Data;

namespace Colportor.Api.Repositories;

/// <summary>
/// Implementação base do repositório
/// </summary>
/// <typeparam name="T">Tipo da entidade</typeparam>
public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public BaseRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task RemoveAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task RemoveByIdAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await RemoveAsync(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return await DbSet.CountAsync();
        
        return await DbSet.CountAsync(predicate);
    }

    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true)
    {
        var query = DbSet.AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();

        if (orderBy != null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
