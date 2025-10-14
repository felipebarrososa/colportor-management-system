using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Implementação do repositório de regiões
/// </summary>
public class RegionRepository : BaseRepository<Region>, IRegionRepository
{
    public RegionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Region?> GetByIdWithRelationsAsync(int id)
    {
        return await DbSet
            .Include(r => r.Country)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Region?> GetByNameAndCountryAsync(string name, int countryId)
    {
        return await DbSet
            .Include(r => r.Country)
            .FirstOrDefaultAsync(r => r.Name == name && r.CountryId == countryId);
    }

    public async Task<IEnumerable<Region>> GetByCountryAsync(int countryId)
    {
        return await DbSet
            .Include(r => r.Country)
            .Where(r => r.CountryId == countryId)
            .ToListAsync();
    }

    public async Task<RegionStats> GetRegionStatsAsync(int regionId)
    {
        var region = await DbSet
            .Include(r => r.Country)
            .FirstOrDefaultAsync(r => r.Id == regionId);

        if (region == null)
            return new RegionStats();

        var totalColportors = await Context.Colportors.CountAsync(c => c.RegionId == regionId);
        var maleColportors = await Context.Colportors.CountAsync(c => c.RegionId == regionId && c.Gender == "Masculino");
        var femaleColportors = await Context.Colportors.CountAsync(c => c.RegionId == regionId && c.Gender == "Feminino");
        var totalLeaders = await Context.Users.CountAsync(u => u.RegionId == regionId && u.Role == "Leader");
        var totalVisits = await Context.Visits
            .Include(v => v.Colportor)
            .CountAsync(v => v.Colportor.RegionId == regionId);
        var totalPacEnrollments = await Context.PacEnrollments
            .Include(p => p.Colportor)
            .CountAsync(p => p.Colportor.RegionId == regionId);

        var lastActivity = await Context.Visits
            .Include(v => v.Colportor)
            .Where(v => v.Colportor.RegionId == regionId)
            .MaxAsync(v => (DateTime?)v.Date);

        return new RegionStats
        {
            Id = region.Id,
            Name = region.Name,
            TotalColportors = totalColportors,
            ActiveColportors = totalColportors, // Implementar lógica de ativo se necessário
            MaleColportors = maleColportors,
            FemaleColportors = femaleColportors,
            TotalLeaders = totalLeaders,
            TotalVisits = totalVisits,
            TotalPacEnrollments = totalPacEnrollments,
            LastActivity = lastActivity ?? DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<Country>> GetCountriesAsync()
    {
        return await Context.Countries
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Country> CreateCountryAsync(string name, string code)
    {
        var country = new Country
        {
            Name = name,
            Iso2 = code
        };

        Context.Countries.Add(country);
        await Context.SaveChangesAsync();

        return country;
    }
}
