using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para endpoints administrativos (legacy compatibility)
/// </summary>
[ApiController]
[Route("admin")]
[Authorize]
public class AdminController : BaseController
{
    private readonly IColportorService _colportorService;
    private readonly ICalendarService _calendarService;
    private readonly IRegionService _regionService;
    private readonly IAuthService _authService;

    public AdminController(
        IColportorService colportorService,
        ICalendarService calendarService,
        IRegionService regionService,
        IAuthService authService,
        ILogger<AdminController> logger) 
        : base(logger)
    {
        _colportorService = colportorService;
        _calendarService = calendarService;
        _regionService = regionService;
        _authService = authService;
    }

    /// <summary>
    /// Lista colportores com paginação (Admin)
    /// </summary>
    [HttpGet("colportors")]
    public async Task<IActionResult> GetColportors(
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 10,
        [FromQuery] string? cpf = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? leaderId = null,
        [FromQuery] string? city = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            Logger.LogInformation("Listando colportores para usuário {UserId} com role {Role}", userId, userRole);

            // Se for um líder, filtrar apenas os colportores vinculados a ele
            if (userRole == "Leader")
            {
                var user = await _authService.GetUserByIdAsync(userId);
                if (user?.RegionId.HasValue == true)
                {
                    regionId = user.RegionId.Value;
                    leaderId = userId; // Forçar filtrar apenas pelos colportores deste líder
                }
            }

            var result = await _colportorService.GetPagedAsync(
                page ?? 1, 
                pageSize ?? 10, 
                regionId, 
                leaderId, 
                null, 
                cpf);
            
            Logger.LogInformation("Retornando {Count} colportores para usuário {UserId}", result.Items.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar colportores");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista inscrições PAC (Admin)
    /// </summary>
    [HttpGet("pac/enrollments")]
    public async Task<IActionResult> GetPacEnrollments(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? leaderId = null)
    {
        try
        {
            var result = await _colportorService.GetPacEnrollmentsAsync(from, to, leaderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar PAC");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém calendário mensal (Admin)
    /// </summary>
    [HttpGet("calendar/monthly")]
    public async Task<IActionResult> GetMonthlyCalendar(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var result = await _calendarService.GetMonthlyCalendarAsync(year, month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar calendário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista todas as regiões (Admin)
    /// </summary>
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions()
    {
        try
        {
            var result = await _regionService.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar regiões");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria nova região (Admin)
    /// </summary>
    [HttpPost("regions")]
    public async Task<IActionResult> CreateRegion([FromBody] RegionCreateDto createDto)
    {
        try
        {
            var result = await _regionService.CreateRegionAsync(createDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar região");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista líderes pendentes de aprovação (Admin)
    /// </summary>
    [HttpGet("leaders/pending")]
    public async Task<IActionResult> GetPendingLeaders()
    {
        try
        {
            var pendingUsers = await _authService.GetPendingUsersAsync();
            return Ok(pendingUsers);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar líderes pendentes");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Aprova um líder pendente (Admin)
    /// </summary>
    [HttpPost("leaders/{userId}/approve")]
    public async Task<IActionResult> ApproveLeader(int userId)
    {
        try
        {
            var result = await _authService.ApproveUserAsync(userId);
            
            if (result.Success)
            {
                Logger.LogInformation("Líder {UserId} aprovado com sucesso", userId);
                return Ok(new { message = "Líder aprovado com sucesso" });
            }

            Logger.LogWarning("Falha ao aprovar líder {UserId}. Motivo: {Reason}", userId, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao aprovar líder {UserId}", userId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista todos os líderes (Admin)
    /// </summary>
    [HttpGet("leaders")]
    public async Task<IActionResult> GetAllLeaders()
    {
        try
        {
            var leaders = await _authService.GetAllLeadersAsync();
            return Ok(leaders);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar líderes");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista colportores para PAC (Admin)
    /// </summary>
    [HttpGet("pac/colportors")]
    public async Task<IActionResult> GetColportorsForPac()
    {
        try
        {
            Logger.LogInformation("Listando colportores para PAC (Admin)");
            var colportors = await _colportorService.GetPagedAsync(1, 1000); // Buscar todos os colportores
            return Ok(colportors.Items);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar colportores para PAC");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Aprova uma solicitação PAC
    /// </summary>
    [HttpPost("pac/enrollments/{id}/approve")]
    public async Task<IActionResult> ApprovePacEnrollment(int id)
    {
        try
        {
            Logger.LogInformation("Aprovando solicitação PAC {EnrollmentId}", id);
            
            var result = await _colportorService.ApprovePacEnrollmentAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            
            return Ok(new { message = "Solicitação aprovada com sucesso" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao aprovar solicitação PAC {EnrollmentId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Rejeita uma solicitação PAC
    /// </summary>
    [HttpPost("pac/enrollments/{id}/reject")]
    public async Task<IActionResult> RejectPacEnrollment(int id)
    {
        try
        {
            Logger.LogInformation("Rejeitando solicitação PAC {EnrollmentId}", id);
            
            var result = await _colportorService.RejectPacEnrollmentAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            
            return Ok(new { message = "Solicitação rejeitada" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao rejeitar solicitação PAC {EnrollmentId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Gera relatório PAC para período específico
    /// </summary>
    [HttpGet("reports/pac")]
    public async Task<IActionResult> GetPacReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? regionId = null)
    {
        try
        {
            Logger.LogInformation("Gerando relatório PAC - De: {StartDate} até: {EndDate}, Região: {RegionId}", 
                startDate, endDate, regionId);

                // Garantir que as datas estejam no formato UTC para PostgreSQL
                // Interpretar as datas recebidas como locais e converter para UTC
                var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Local).ToUniversalTime();
                var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Local).ToUniversalTime();

            var enrollments = await _colportorService.GetPacEnrollmentsAsync(utcStartDate, utcEndDate, regionId);
            
            // Filtrar apenas solicitações aprovadas para o relatório
            var approvedEnrollments = enrollments.Where(e => e.Status == "Approved").ToList();
            
            var reportData = approvedEnrollments.Select(e => new
            {
                id = e.Id,
                colportorName = e.ColportorName,
                region = e.ColportorRegionName ?? "Região não informada",
                gender = e.ColportorGender ?? "Não informado",
                startDate = e.StartDate.ToString("yyyy-MM-dd"),
                endDate = e.EndDate.ToString("yyyy-MM-dd"),
                leaderName = e.LeaderName,
                status = e.Status,
                createdAt = e.CreatedAt
            }).ToList();

            return Ok(reportData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao gerar relatório PAC");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Criar nova visita (check-in)
    /// </summary>
    [HttpPost("visits")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateVisit([FromBody] CreateVisitDto createDto)
    {
        try
        {
            Logger.LogInformation("Criando visita para colportor {ColportorId}", createDto.ColportorId);

            var result = await _colportorService.CreateVisitAsync(createDto.ColportorId, createDto.Date);
            
            if (result.Success)
            {
                return Ok(new { message = "Visita registrada com sucesso", visitId = result.Data?.Id });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar visita");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
