using Colportor.Api.DTOs;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviços do calendário PAC
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Obtém dados do calendário mensal
    /// </summary>
    Task<MonthlyCalendarDto> GetMonthlyCalendarAsync(int year, int month);

    /// <summary>
    /// Obtém estatísticas de um dia específico
    /// </summary>
    Task<DayStatsDto?> GetDayStatsAsync(DateTime date);

    /// <summary>
    /// Obtém estatísticas de uma região em um dia específico
    /// </summary>
    Task<RegionDayStatsDto?> GetRegionDayStatsAsync(DateTime date, int regionId);

    /// <summary>
    /// Obtém estatísticas gerais do calendário
    /// </summary>
    Task<CalendarStatsDto> GetCalendarStatsAsync(int? year = null, int? month = null);
}

/// <summary>
/// DTO para calendário mensal
/// </summary>
public class MonthlyCalendarDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public Dictionary<string, DayStatsDto> CalendarData { get; set; } = new();
}

/// <summary>
/// DTO para estatísticas do dia
/// </summary>
public class DayStatsDto
{
    public string Date { get; set; } = string.Empty;
    public int Males { get; set; }
    public int Females { get; set; }
    public int Total { get; set; }
    public List<RegionDayStatsDto> Regions { get; set; } = new();
}

/// <summary>
/// DTO para estatísticas da região em um dia
/// </summary>
public class RegionDayStatsDto
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public int Males { get; set; }
    public int Females { get; set; }
    public int Total { get; set; }
    public List<ColportorSummaryDto> Colportors { get; set; } = new();
}

/// <summary>
/// DTO para resumo do colportor
/// </summary>
public class ColportorSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para estatísticas gerais do calendário
/// </summary>
public class CalendarStatsDto
{
    public int TotalDays { get; set; }
    public int DaysWithData { get; set; }
    public int TotalColportors { get; set; }
    public int TotalMales { get; set; }
    public int TotalFemales { get; set; }
    public Dictionary<string, int> ColportorsByRegion { get; set; } = new();
    public Dictionary<string, int> ColportorsByDay { get; set; } = new();
}
