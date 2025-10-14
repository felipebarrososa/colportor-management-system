using AutoMapper;
using Colportor.Api.Data;
using Colportor.Api.DTOs;
using Colportor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Colportor.Api.Services;

/// <summary>
/// Implementação do serviço do calendário PAC
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CalendarService> _logger;
    private readonly ICacheService _cacheService;

    public CalendarService(AppDbContext context, IMapper mapper, ILogger<CalendarService> logger, ICacheService cacheService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<MonthlyCalendarDto> GetMonthlyCalendarAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("Obtendo calendário mensal para {Year}-{Month}", year, month);

            // Cache temporariamente desabilitado para garantir dados atualizados
            // var cacheKey = $"calendar:monthly:{year}-{month}";
            // var cachedResult = await _cacheService.GetAsync<MonthlyCalendarDto>(cacheKey);
            // if (cachedResult != null)
            // {
            //     _logger.LogDebug("Calendário obtido do cache para {Year}-{Month}", year, month);
            //     return cachedResult;
            // }

            var firstDay = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var enrollments = await _context.PacEnrollments
                .Include(p => p.Colportor)
                    .ThenInclude(c => c.Region)
                .Include(p => p.Leader)
                .Where(p => p.StartDate <= lastDay && p.EndDate >= firstDay && p.Status == "Approved")
                .ToListAsync();

            var calendarData = new Dictionary<string, DayStatsDto>();

            // Nomes dos meses em português
            var monthNames = new[]
            {
                "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
                "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
            };

            // Processar cada dia do mês
            for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var dayEnrollments = enrollments.Where(e => 
                    e.StartDate <= date && e.EndDate >= date).ToList();

                var regions = dayEnrollments
                    .GroupBy(e => new { e.Colportor.RegionId, e.Colportor.Region!.Name })
                    .Select(g => new RegionDayStatsDto
                    {
                        RegionId = g.Key.RegionId ?? 0,
                        RegionName = g.Key.Name,
                        Males = g.Count(e => e.Colportor.Gender == "Masculino"),
                        Females = g.Count(e => e.Colportor.Gender == "Feminino"),
                        Total = g.Count(),
                        Colportors = g.Select(e => _mapper.Map<ColportorSummaryDto>(e.Colportor)).ToList()
                    })
                    .ToList();

                calendarData[dateKey] = new DayStatsDto
                {
                    Date = dateKey,
                    Males = regions.Sum(r => r.Males),
                    Females = regions.Sum(r => r.Females),
                    Total = regions.Sum(r => r.Total),
                    Regions = regions
                };
            }

            var result = new MonthlyCalendarDto
            {
                Year = year,
                Month = month,
                MonthName = monthNames[month - 1],
                CalendarData = calendarData
            };

            // Cache temporariamente desabilitado
            // await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            _logger.LogInformation("Calendário mensal gerado com {Days} dias com dados", calendarData.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter calendário mensal para {Year}-{Month}", year, month);
            throw;
        }
    }

    public async Task<DayStatsDto?> GetDayStatsAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Obtendo estatísticas do dia {Date}", date.ToString("yyyy-MM-dd"));

            var enrollments = await _context.PacEnrollments
                .Include(p => p.Colportor)
                    .ThenInclude(c => c.Region)
                .Include(p => p.Leader)
                .Where(p => p.StartDate <= date && p.EndDate >= date && p.Status == "Approved")
                .ToListAsync();

            if (!enrollments.Any())
                return null;

            var regions = enrollments
                .GroupBy(e => new { e.Colportor.RegionId, e.Colportor.Region!.Name })
                .Select(g => new RegionDayStatsDto
                {
                    RegionId = g.Key.RegionId ?? 0,
                    RegionName = g.Key.Name,
                    Males = g.Count(e => e.Colportor.Gender == "Masculino"),
                    Females = g.Count(e => e.Colportor.Gender == "Feminino"),
                    Total = g.Count(),
                    Colportors = g.Select(e => _mapper.Map<ColportorSummaryDto>(e.Colportor)).ToList()
                })
                .ToList();

            return new DayStatsDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Males = regions.Sum(r => r.Males),
                Females = regions.Sum(r => r.Females),
                Total = regions.Sum(r => r.Total),
                Regions = regions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas do dia {Date}", date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task<RegionDayStatsDto?> GetRegionDayStatsAsync(DateTime date, int regionId)
    {
        try
        {
            _logger.LogInformation("Obtendo estatísticas da região {RegionId} para {Date}", regionId, date.ToString("yyyy-MM-dd"));

            var enrollments = await _context.PacEnrollments
                .Include(p => p.Colportor)
                    .ThenInclude(c => c.Region)
                .Include(p => p.Leader)
                .Where(p => p.StartDate <= date && 
                           p.EndDate >= date && 
                           p.Colportor.RegionId == regionId &&
                           p.Status == "Approved")
                .ToListAsync();

            if (!enrollments.Any())
                return null;

            var region = enrollments.First().Colportor.Region!;

            return new RegionDayStatsDto
            {
                RegionId = regionId,
                RegionName = region.Name,
                Males = enrollments.Count(e => e.Colportor.Gender == "Masculino"),
                Females = enrollments.Count(e => e.Colportor.Gender == "Feminino"),
                Total = enrollments.Count,
                Colportors = enrollments.Select(e => _mapper.Map<ColportorSummaryDto>(e.Colportor)).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas da região {RegionId} para {Date}", regionId, date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task<CalendarStatsDto> GetCalendarStatsAsync(int? year = null, int? month = null)
    {
        try
        {
            _logger.LogInformation("Obtendo estatísticas gerais do calendário");

            var query = _context.PacEnrollments
                .Include(p => p.Colportor)
                    .ThenInclude(c => c.Region)
                .AsQueryable();

            if (year.HasValue)
            {
                var startOfYear = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfYear = new DateTime(year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                query = query.Where(p => p.StartDate >= startOfYear && p.StartDate <= endOfYear);
            }

            if (month.HasValue && year.HasValue)
            {
                var startOfMonth = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                query = query.Where(p => p.StartDate >= startOfMonth && p.StartDate <= endOfMonth);
            }

            var enrollments = await query.ToListAsync();

            var totalDays = year.HasValue && month.HasValue 
                ? DateTime.DaysInMonth(year.Value, month.Value)
                : 365;

            var daysWithData = enrollments
                .Select(e => e.StartDate.Date)
                .Distinct()
                .Count();

            var colportorsByRegion = enrollments
                .GroupBy(e => e.Colportor.Region!.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var colportorsByDay = enrollments
                .GroupBy(e => e.StartDate.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            return new CalendarStatsDto
            {
                TotalDays = totalDays,
                DaysWithData = daysWithData,
                TotalColportors = enrollments.Count,
                TotalMales = enrollments.Count(e => e.Colportor.Gender == "Masculino"),
                TotalFemales = enrollments.Count(e => e.Colportor.Gender == "Feminino"),
                ColportorsByRegion = colportorsByRegion,
                ColportorsByDay = colportorsByDay
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas gerais do calendário");
            throw;
        }
    }
}
