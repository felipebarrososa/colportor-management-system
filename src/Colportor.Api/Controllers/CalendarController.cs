using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para funcionalidades do calendário PAC
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CalendarController : BaseController
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger) 
        : base(logger)
    {
        _calendarService = calendarService;
    }

    /// <summary>
    /// Obter dados do calendário mensal
    /// </summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyCalendar([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            Logger.LogInformation("Solicitação de calendário para {Year}-{Month}", year, month);
            
            var calendarData = await _calendarService.GetMonthlyCalendarAsync(year, month);
            
            return Ok(calendarData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter calendário mensal para {Year}-{Month}", year, month);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter estatísticas de um dia específico
    /// </summary>
    [HttpGet("day/{date}")]
    public async Task<IActionResult> GetDayStats(string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                return BadRequest(new { message = "Data inválida. Use o formato yyyy-MM-dd" });
            }

            Logger.LogInformation("Solicitação de estatísticas para {Date}", date);
            
            var dayStats = await _calendarService.GetDayStatsAsync(targetDate);
            
            if (dayStats == null)
            {
                return NotFound(new { message = "Nenhum dado encontrado para esta data" });
            }

            return Ok(dayStats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter estatísticas do dia {Date}", date);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter estatísticas de uma região em um dia específico
    /// </summary>
    [HttpGet("day/{date}/region/{regionId}")]
    public async Task<IActionResult> GetRegionDayStats(string date, int regionId)
    {
        try
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                return BadRequest(new { message = "Data inválida. Use o formato yyyy-MM-dd" });
            }

            Logger.LogInformation("Solicitação de estatísticas da região {RegionId} para {Date}", regionId, date);
            
            var regionStats = await _calendarService.GetRegionDayStatsAsync(targetDate, regionId);
            
            if (regionStats == null)
            {
                return NotFound(new { message = "Nenhum dado encontrado para esta região e data" });
            }

            return Ok(regionStats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter estatísticas da região {RegionId} para {Date}", regionId, date);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter estatísticas gerais do calendário
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetCalendarStats([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        try
        {
            Logger.LogInformation("Solicitação de estatísticas gerais do calendário");
            
            var stats = await _calendarService.GetCalendarStatsAsync(year, month);
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter estatísticas gerais do calendário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
