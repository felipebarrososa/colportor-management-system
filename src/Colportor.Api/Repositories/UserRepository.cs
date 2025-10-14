using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Implementação do repositório de usuários
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdWithRelationsAsync(int id)
    {
        return await DbSet
            .Include(u => u.Colportor)
                .ThenInclude(c => c.Leader) // Incluir o líder do colportor
            .Include(u => u.Region)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        return await DbSet
            .Include(u => u.Colportor)
            .Include(u => u.Region)
            .Where(u => u.Role == role)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await DbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetByRegionAsync(int regionId)
    {
        return await DbSet
            .Include(u => u.Colportor)
            .Include(u => u.Region)
            .Where(u => u.RegionId == regionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetPendingUsersAsync()
    {
        return await DbSet
            .Include(u => u.Region)
            .Where(u => u.Role == "Pending")
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public AppDbContext GetContext()
    {
        return Context;
    }
}
