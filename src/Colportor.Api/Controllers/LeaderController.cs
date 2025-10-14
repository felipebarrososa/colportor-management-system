using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para funcionalidades específicas dos líderes
/// </summary>
[ApiController]
[Route("[controller]")] // Rota base /leader
[Authorize(Roles = "Leader")] // Requer autenticação e role Leader
public class LeaderController : BaseController
{
    private readonly IColportorService _colportorService;
    private readonly IAuthService _authService;

    public LeaderController(
        IColportorService colportorService,
        IAuthService authService,
        ILogger<LeaderController> logger) : base(logger)
    {
        _colportorService = colportorService;
        _authService = authService;
    }

    /// <summary>
    /// Lista colportores do líder para seleção no PAC
    /// </summary>
    [HttpGet("pac/colportors")]
    public async Task<IActionResult> GetColportorsForPac()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            Logger.LogInformation("Listando colportores para PAC do líder {LeaderId}", userId);

            // Buscar colportores associados ao líder
            var result = await _colportorService.GetColportorsAsync(userId, "Leader", 1, 100);
            
            Logger.LogInformation("Encontrados {Count} colportores para o líder {LeaderId}", result.Items.Count(), userId);
            
            return Ok(result.Items);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar colportores para PAC");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista inscrições PAC do líder
    /// </summary>
    [HttpGet("pac/enrollments")]
    public async Task<IActionResult> GetPacEnrollments()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            Logger.LogInformation("Listando inscrições PAC do líder {LeaderId}", userId);

            var enrollments = await _colportorService.GetPacEnrollmentsAsync(null, null, userId);
            
            Logger.LogInformation("Encontradas {Count} inscrições PAC para o líder {LeaderId}", enrollments.Count(), userId);
            
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar inscrições PAC do líder");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria nova inscrição PAC (envia para aprovação)
    /// </summary>
    [HttpPost("pac/enrollments")]
    public async Task<IActionResult> CreatePacEnrollment([FromBody] CreatePacEnrollmentDto createDto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            Logger.LogInformation("Criando inscrição PAC para líder {LeaderId} com {Count} colportores", userId, createDto.ColportorIds.Count);
            Logger.LogInformation("Datas recebidas - StartDate: {StartDate}, EndDate: {EndDate}", createDto.StartDate, createDto.EndDate);

            // Garantir que as datas estejam em UTC para PostgreSQL
            // Interpretar as datas recebidas como locais e converter para UTC
            var utcStartDate = DateTime.SpecifyKind(createDto.StartDate, DateTimeKind.Local).ToUniversalTime();
            var utcEndDate = DateTime.SpecifyKind(createDto.EndDate, DateTimeKind.Local).ToUniversalTime();

            // Criar inscrições PAC usando o serviço
            var result = await _colportorService.CreatePacEnrollmentAsync(
                userId, 
                createDto.ColportorIds, 
                utcStartDate, 
                utcEndDate
            );

            if (!result.Success)
            {
                Logger.LogWarning("Falha ao criar inscrição PAC para líder {LeaderId}: {Error}", userId, result.Message);
                return BadRequest(new { message = result.Message });
            }

            Logger.LogInformation("Inscrição PAC criada com sucesso para líder {LeaderId} com {Count} colportores", userId, createDto.ColportorIds.Count);
            return Ok(new { message = "Solicitação enviada para aprovação do PAC" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar inscrição PAC");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}

/// <summary>
/// DTO para criação de inscrição PAC
/// </summary>
public class CreatePacEnrollmentDto
{
    public List<int> ColportorIds { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
