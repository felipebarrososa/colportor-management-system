using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de regiões
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegionController : BaseController
{
    private readonly IRegionService _regionService;

    public RegionController(IRegionService regionService, ILogger<RegionController> logger) 
        : base(logger)
    {
        _regionService = regionService;
    }

    /// <summary>
    /// Listar todas as regiões
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRegions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var regions = await _regionService.GetRegionsAsync(userId, userRole);
            
            return Ok(regions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar regiões");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter região por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRegion(int id)
    {
        try
        {
            var region = await _regionService.GetRegionByIdAsync(id);
            
            if (region == null)
            {
                return NotFound(new { message = "Região não encontrada" });
            }

            return Ok(region);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter região {RegionId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Criar nova região (Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRegion([FromBody] RegionCreateDto createDto)
    {
        try
        {
            var result = await _regionService.CreateRegionAsync(createDto);
            
            if (result.Success)
            {
                Logger.LogInformation("Região criada com sucesso: {RegionId}", result.Data?.Id);
                return CreatedAtAction(nameof(GetRegion), new { id = result.Data?.Id }, result);
            }

            Logger.LogWarning("Falha ao criar região. Motivo: {Reason}", result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar região");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualizar região (Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRegion(int id, [FromBody] RegionCreateDto updateDto)
    {
        try
        {
            var result = await _regionService.UpdateRegionAsync(id, updateDto);
            
            if (result.Success)
            {
                Logger.LogInformation("Região atualizada com sucesso: {RegionId}", id);
                return Ok(result);
            }

            if (result.Message.Contains("não encontrada"))
            {
                return NotFound(new { message = result.Message });
            }

            Logger.LogWarning("Falha ao atualizar região {RegionId}. Motivo: {Reason}", id, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar região {RegionId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deletar região (Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        try
        {
            var result = await _regionService.DeleteRegionAsync(id);
            
            if (result.Success)
            {
                Logger.LogInformation("Região deletada com sucesso: {RegionId}", id);
                return Ok(new { message = "Região deletada com sucesso" });
            }

            if (result.Message.Contains("não encontrada"))
            {
                return NotFound(new { message = result.Message });
            }

            Logger.LogWarning("Falha ao deletar região {RegionId}. Motivo: {Reason}", id, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao deletar região {RegionId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter estatísticas de uma região
    /// </summary>
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetRegionStats(int id)
    {
        try
        {
            var stats = await _regionService.GetRegionStatsAsync(id);
            
            if (stats == null)
            {
                return NotFound(new { message = "Região não encontrada" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter estatísticas da região {RegionId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
