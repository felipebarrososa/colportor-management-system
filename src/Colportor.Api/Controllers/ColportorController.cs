using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de colportores
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ColportorController : BaseController
{
    private readonly IColportorService _colportorService;

    public ColportorController(IColportorService colportorService, ILogger<ColportorController> logger) 
        : base(logger)
    {
        _colportorService = colportorService;
    }

    /// <summary>
    /// Listar todos os colportores (Admin e Leader)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetColportors([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _colportorService.GetColportorsAsync(userId, userRole, page, pageSize);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar colportores");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter colportor por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetColportor(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var colportor = await _colportorService.GetColportorByIdAsync(id, userId, userRole);
            
            if (colportor == null)
            {
                return NotFound(new { message = "Colportor não encontrado" });
            }

            return Ok(colportor);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter colportor {ColportorId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Criar novo colportor (Admin e Leader)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Leader")]
    public async Task<IActionResult> CreateColportor([FromBody] CreateColportorDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _colportorService.CreateColportorAsync(createDto, userId, userRole);
            
            if (result.Success)
            {
                Logger.LogInformation("Colportor criado com sucesso: {ColportorId}", result.Data?.Id);
                return CreatedAtAction(nameof(GetColportor), new { id = result.Data?.Id }, result);
            }

            Logger.LogWarning("Falha ao criar colportor. Motivo: {Reason}", result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar colportor");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualizar colportor (Admin e Leader)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Leader")]
    public async Task<IActionResult> UpdateColportor(int id, [FromBody] UpdateColportorDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _colportorService.UpdateColportorAsync(id, updateDto, userId, userRole);
            
            if (result.Success)
            {
                Logger.LogInformation("Colportor atualizado com sucesso: {ColportorId}", id);
                return Ok(result);
            }

            if (result.Message.Contains("não encontrado"))
            {
                return NotFound(new { message = result.Message });
            }

            Logger.LogWarning("Falha ao atualizar colportor {ColportorId}. Motivo: {Reason}", id, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar colportor {ColportorId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deletar colportor (Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteColportor(int id)
    {
        try
        {
            var result = await _colportorService.DeleteColportorAsync(id);
            
            if (result.Success)
            {
                Logger.LogInformation("Colportor deletado com sucesso: {ColportorId}", id);
                return Ok(new { message = "Colportor deletado com sucesso" });
            }

            if (result.Message.Contains("não encontrado"))
            {
                return NotFound(new { message = result.Message });
            }

            Logger.LogWarning("Falha ao deletar colportor {ColportorId}. Motivo: {Reason}", id, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao deletar colportor {ColportorId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter estatísticas de colportores (Admin)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetColportorStats()
    {
        try
        {
            var stats = await _colportorService.GetColportorStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter estatísticas de colportores");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
