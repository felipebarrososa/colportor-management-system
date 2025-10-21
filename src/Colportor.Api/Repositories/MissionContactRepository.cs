using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Colportor.Api.Repositories
{
    public class MissionContactRepository : BaseRepository<MissionContact>, IMissionContactRepository
    {
        public MissionContactRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<MissionContact> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            int? regionId = null,
            int? leaderId = null,
            string? status = null,
            string? searchTerm = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null,
            DateTime? availableFrom = null,
            DateTime? availableTo = null)
        {
            var query = DbSet
                .Include(x => x.Region)
                .Include(x => x.Leader)
                .AsQueryable();

            if (regionId.HasValue) query = query.Where(x => x.RegionId == regionId.Value);
            if (leaderId.HasValue) query = query.Where(x => x.LeaderId == leaderId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.FullName.Contains(searchTerm) ||
                    (x.Email != null && x.Email.Contains(searchTerm)) ||
                    (x.Phone != null && x.Phone.Contains(searchTerm)) ||
                    (x.City != null && x.City.Contains(searchTerm)));
            }
            if (createdFrom.HasValue) query = query.Where(x => x.CreatedAt >= createdFrom.Value);
            if (createdTo.HasValue) query = query.Where(x => x.CreatedAt <= createdTo.Value);
            if (availableFrom.HasValue) query = query.Where(x => x.AvailableDate >= availableFrom.Value);
            if (availableTo.HasValue) query = query.Where(x => x.AvailableDate <= availableTo.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}



