using Microsoft.AspNetCore.Mvc;
using Colportor.Api.Services;
using Colportor.Api.DTOs;
using Colportor.Api.Models;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para autenticação e autorização
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger) 
        : base(logger)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login de usuário
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            Logger.LogInformation("Tentativa de login para email: {Email}", loginDto.Email);
            
            var result = await _authService.LoginAsync(loginDto.Email, loginDto.Password);
            
            if (result.Success)
            {
                Logger.LogInformation("Login bem-sucedido para email: {Email}", loginDto.Email);
                return Ok(result);
            }

            Logger.LogWarning("Falha no login para email: {Email}", loginDto.Email);
            return Unauthorized(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante login para email: {Email}", loginDto.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Registro de novo usuário
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            Logger.LogInformation("Tentativa de registro para email: {Email}", registerDto.Email);
            
            var result = await _authService.RegisterAsync(registerDto);
            
            if (result.Success)
            {
                Logger.LogInformation("Registro bem-sucedido para email: {Email}", registerDto.Email);
                return Ok(result);
            }

            Logger.LogWarning("Falha no registro para email: {Email}. Motivo: {Reason}", 
                registerDto.Email, result.Message);
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante registro para email: {Email}", registerDto.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Refresh do token JWT
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var result = await _authService.RefreshTokenAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante refresh do token para usuário: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Logout do usuário
    /// </summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            Logger.LogInformation("Logout realizado para usuário: {UserId}", userId);
            
            // Aqui você pode implementar blacklist de tokens se necessário
            return Ok(new { message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante logout para usuário: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter informações do usuário logado
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                FullName = user.FullName,
                CPF = user.CPF,
                City = user.City,
                RegionId = user.RegionId,
                ColportorId = user.ColportorId
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter informações do usuário: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
