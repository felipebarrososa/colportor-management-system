using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.Models;

namespace Colportor.Api.Repositories;

/// <summary>
/// Implementação do repositório de colportores
/// </summary>
public class ColportorRepository : BaseRepository<Models.Colportor>, IColportorRepository
{
    public ColportorRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Models.Colportor?> GetByIdWithRelationsAsync(int id)
    {
        return await DbSet
            .Include(c => c.Region)
            .Include(c => c.Leader)
            .Include(c => c.Visits)
            .Include(c => c.PacEnrollments)
                .ThenInclude(p => p.Leader)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Models.Colportor>> GetByRegionAsync(int regionId)
    {
        return await DbSet
            .Include(c => c.Region)
            .Include(c => c.Leader)
            .Where(c => c.RegionId == regionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Models.Colportor>> GetByLeaderAsync(int leaderId)
    {
        return await DbSet
            .Include(c => c.Region)
            .Include(c => c.Leader)
            .Where(c => c.LeaderId == leaderId)
            .ToListAsync();
    }

    public async Task<Models.Colportor?> GetByCPFAsync(string cpf)
    {
        return await DbSet
            .Include(c => c.Region)
            .Include(c => c.Leader)
            .FirstOrDefaultAsync(c => c.CPF == cpf);
    }

    public async Task<ColportorStats> GetColportorStatsAsync()
    {
        var totalColportors = await DbSet.CountAsync();
        var maleColportors = await DbSet.CountAsync(c => c.Gender == "Masculino");
        var femaleColportors = await DbSet.CountAsync(c => c.Gender == "Feminino");

        var colportorsByRegion = await DbSet
            .Include(c => c.Region)
            .GroupBy(c => c.Region!.Name)
            .Select(g => new { Region = g.Key ?? "Sem região", Count = g.Count() })
            .ToDictionaryAsync(x => x.Region, x => x.Count);

        var colportorsByGender = new Dictionary<string, int>
        {
            ["Masculino"] = maleColportors,
            ["Feminino"] = femaleColportors
        };

        return new ColportorStats
        {
            TotalColportors = totalColportors,
            ActiveColportors = totalColportors, // Implementar lógica de ativo se necessário
            MaleColportors = maleColportors,
            FemaleColportors = femaleColportors,
            ColportorsByRegion = colportorsByRegion,
            ColportorsByGender = colportorsByGender
        };
    }

    public async Task<(IEnumerable<Models.Colportor> Items, int TotalCount)> GetPagedWithFiltersAsync(
        int page, 
        int pageSize, 
        int? regionId = null,
        int? leaderId = null,
        string? gender = null,
        string? searchTerm = null)
    {
        var query = DbSet
            .Include(c => c.Region)
            .Include(c => c.Leader)
            .AsQueryable();

        // Aplicar filtros
        if (regionId.HasValue)
            query = query.Where(c => c.RegionId == regionId.Value);

        if (leaderId.HasValue)
            query = query.Where(c => c.LeaderId == leaderId.Value);

        if (!string.IsNullOrEmpty(gender))
            query = query.Where(c => c.Gender == gender);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(c => 
                c.FullName.Contains(searchTerm) || 
                c.CPF.Contains(searchTerm) ||
                c.City!.Contains(searchTerm));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Models.PacEnrollment>> GetPacEnrollmentsAsync(DateTime? from = null, DateTime? to = null, int? leaderId = null)
    {
        var query = Context.PacEnrollments.AsQueryable();

        if (from.HasValue && to.HasValue)
        {
            // Para relatórios: buscar solicitações que CHEGAM no período solicitado
            // Filtra por StartDate (dia de chegada) dentro do período
            query = query.Where(p => p.StartDate >= from.Value && p.StartDate <= to.Value);
        }
        else
        {
            // Lógica original para outros casos
            if (from.HasValue)
                query = query.Where(p => p.StartDate >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.EndDate <= to.Value);
        }

        if (leaderId.HasValue)
            query = query.Where(p => p.LeaderId == leaderId.Value);

        return await query
            .Include(p => p.Leader)
            .Include(p => p.Colportor)
                .ThenInclude(c => c.Region)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Models.PacEnrollment> AddPacEnrollmentAsync(Models.PacEnrollment enrollment)
    {
        Context.PacEnrollments.Add(enrollment);
        await Context.SaveChangesAsync();
        return enrollment;
    }

    public async Task<bool> ApprovePacEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await Context.PacEnrollments
            .FirstOrDefaultAsync(p => p.Id == enrollmentId);

        if (enrollment == null || enrollment.Status != "Pending")
        {
            return false;
        }

        enrollment.Status = "Approved";
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectPacEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await Context.PacEnrollments
            .FirstOrDefaultAsync(p => p.Id == enrollmentId);

        if (enrollment == null || enrollment.Status != "Pending")
        {
            return false;
        }

        enrollment.Status = "Rejected";
        await Context.SaveChangesAsync();
        return true;
    }

    public AppDbContext GetContext()
    {
        return Context;
    }
}
