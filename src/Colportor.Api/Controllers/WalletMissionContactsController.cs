using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Endpoints do Wallet para colportores criarem/visualizarem seus contatos missionários
/// </summary>
[ApiController]
[Route("wallet/mission-contacts")]
[Authorize]
public class WalletMissionContactsController : BaseController
{
    private readonly IMissionContactService _service;

    public WalletMissionContactsController(IMissionContactService service, ILogger<WalletMissionContactsController> logger)
        : base(logger)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] MissionContactFilterDto filter)
    {
        try
        {
            var result = await _service.GetPagedAsync(filter, GetCurrentUserId(), GetCurrentUserRole());
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar contatos missionários no wallet");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            if (item == null) return NotFound(new { message = "Contato não encontrado" });
            return Ok(item);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter contato missionário no wallet {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MissionContactCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto, GetCurrentUserId(), GetCurrentUserRole());
            if (result.Success) return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar contato missionário no wallet");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}



