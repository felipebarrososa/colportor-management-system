using Colportor.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Colportor.Api.Repositories
{
    public interface IMissionContactRepository : IRepository<MissionContact>
    {
        Task<(IEnumerable<MissionContact> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            int? regionId = null,
            int? leaderId = null,
            string? status = null,
            string? searchTerm = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null,
            DateTime? availableFrom = null,
            DateTime? availableTo = null);
    }
}



