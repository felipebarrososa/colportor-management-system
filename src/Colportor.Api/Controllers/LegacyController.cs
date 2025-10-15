using Microsoft.AspNetCore.Mvc;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para rotas legacy de compatibilidade com frontend
/// </summary>
[ApiController]
public class LegacyController : BaseController
{
    private readonly IAuthService _authService;

    public LegacyController(IAuthService authService, ILogger<LegacyController> logger) 
        : base(logger)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registro de líder (rota legacy)
    /// </summary>
    [HttpPost("/leaders/register")]
    public async Task<IActionResult> RegisterLeader([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            
            if (result.Success)
            {
                Logger.LogInformation("Registro de líder realizado com sucesso para: {Email}", registerDto.Email);
                return Ok(new { message = "Solicitação de líder enviada com sucesso. Aguarde aprovação." });
            }

            Logger.LogWarning("Falha no registro de líder para: {Email}. Motivo: {Reason}", registerDto.Email, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante registro de líder para: {Email}", registerDto.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Registro de colportor (rota legacy)
    /// </summary>
    [HttpPost("/auth/register")]
    public async Task<IActionResult> RegisterColportor([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterColportorAsync(registerDto);
            
            if (result.Success)
            {
                Logger.LogInformation("Registro de colportor realizado com sucesso para: {Email}", registerDto.Email);
                return Ok(new { message = "Colportor cadastrado com sucesso." });
            }

            Logger.LogWarning("Falha no registro de colportor para: {Email}. Motivo: {Reason}", registerDto.Email, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante registro de colportor para: {Email}", registerDto.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
