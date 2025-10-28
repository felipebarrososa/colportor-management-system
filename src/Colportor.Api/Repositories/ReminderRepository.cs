using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Implementação do repositório de lembretes
/// </summary>
public class ReminderRepository : BaseRepository<Reminder>, IReminderRepository
{
    public ReminderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Reminder>> GetByContactIdAsync(int contactId)
    {
        return await DbSet
            .Where(r => r.ContactId == contactId)
            .OrderBy(r => r.DateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId)
    {
        return await DbSet
            .Include(r => r.Contact)
            .Where(r => r.Contact != null && r.Contact.CreatedByUserId == userId)
            .OrderBy(r => r.DateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetTodayByUserIdAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet
            .Include(r => r.Contact)
            .Where(r => r.Contact != null && 
                       r.Contact.CreatedByUserId == userId &&
                       r.DateTime.Date == today && 
                       !r.Completed)
            .OrderBy(r => r.DateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetOverdueByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Include(r => r.Contact)
            .Where(r => r.Contact != null && 
                       r.Contact.CreatedByUserId == userId &&
                       r.DateTime < now && 
                       !r.Completed)
            .OrderBy(r => r.DateTime)
            .ToListAsync();
    }

    public async Task<int> GetPendingCountByContactIdAsync(int contactId)
    {
        return await DbSet
            .CountAsync(r => r.ContactId == contactId && !r.Completed);
    }

    public async Task<Reminder?> GetByIdWithRelationsAsync(int id)
    {
        return await DbSet
            .Include(r => r.Contact)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}


