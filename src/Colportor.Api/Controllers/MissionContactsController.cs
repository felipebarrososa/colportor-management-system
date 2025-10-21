using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services;
using Colportor.Api.Services.Interfaces;
using Colportor.Api.DTOs;
using Colportor.Api.Models;

namespace Colportor.Api.Controllers;

/// <summary>
/// API para gerenciamento de contatos missionários (Admin/Leader)
/// </summary>
[ApiController]
[Route("api/mission-contacts")]
[Authorize]
public class MissionContactsController : BaseController
{
    private readonly IMissionContactService _service;
    private readonly IWhatsAppService _whatsAppService;

    public MissionContactsController(IMissionContactService service, IWhatsAppService whatsAppService, ILogger<MissionContactsController> logger)
        : base(logger)
    {
        _service = service;
        _whatsAppService = whatsAppService;
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
            Logger.LogError(ex, "Erro ao listar contatos missionários");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost("{id}/notify/whatsapp")]
    [Authorize(Roles = "Admin,Leader")]
    public async Task<IActionResult> SendWhatsApp(int id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            if (item == null) return NotFound(new { message = "Contato não encontrado" });
            // WhatsApp integration - placeholder
            return Ok(new { message = "WhatsApp integration em desenvolvimento" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao enviar WhatsApp para contato {Id}", id);
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
            Logger.LogError(ex, "Erro ao obter contato missionário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Leader")] // Admin/Leader podem criar pelo admin
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
            Logger.LogError(ex, "Erro ao criar contato missionário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Leader")] // Admin/Leader podem editar
    public async Task<IActionResult> Update(int id, [FromBody] MissionContactUpdateDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto, GetCurrentUserId(), GetCurrentUserRole());
            if (result.Success) return Ok(result.Data);
            if (result.Message.Contains("não encontrado")) return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar contato missionário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Leader")] // Admin/Leader podem mover no pipeline
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] MissionContactStatusUpdateDto dto)
    {
        try
        {
            var result = await _service.UpdateStatusAsync(id, dto, GetCurrentUserId(), GetCurrentUserRole());
            if (result.Success) return Ok(result.Data);
            if (result.Message.Contains("não encontrado")) return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar status do contato missionário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPut("{id}/assign")]
    [Authorize(Roles = "Admin,Leader")] // Admin pode atribuir qualquer; Leader só para si
    public async Task<IActionResult> Assign(int id, [FromBody] MissionContactAssignDto dto)
    {
        try
        {
            var result = await _service.AssignLeaderAsync(id, dto.LeaderId, GetCurrentUserId(), GetCurrentUserRole());
            if (result.Success) return Ok(result.Data);
            if (result.Message.Contains("não encontrado")) return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atribuir líder ao contato missionário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // apenas Admin remove definitivamente
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            if (result.Success) return Ok(new { message = "Contato removido" });
            if (result.Message.Contains("não encontrado")) return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao remover contato missionário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}


