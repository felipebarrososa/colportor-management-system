using Microsoft.AspNetCore.Mvc;
using Colportor.Api.Services;
using Colportor.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para autenticação e registro
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger) 
        : base(logger)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login do usuário
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto.Email, loginDto.Password);
            
            if (result.Success)
            {
                Logger.LogInformation("Login realizado com sucesso para: {Email}", loginDto.Email);
                return Ok(result.Data);
            }

            Logger.LogWarning("Tentativa de login falhada para: {Email}", loginDto.Email);
            return Unauthorized(new { message = result.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro durante login para: {Email}", loginDto.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém dados do usuário logado (para wallet)
    /// </summary>
    [HttpGet("wallet/me")]
    [Route("wallet/me")]
    [Route("/wallet/me")]
    [Authorize]
    public async Task<IActionResult> GetWalletUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            var userDto = new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role,
                cpf = user.CPF,
                city = user.City,
                regionId = user.RegionId,
                colportorId = user.ColportorId,
                status = user.Colportor != null ? CalculateStatus(user.Colportor.LastVisitDate) : "PENDENTE",
                // Dados do colportor se existir
                colportor = user.Colportor != null ? new
                {
                    id = user.Colportor.Id,
                    fullName = user.Colportor.FullName,
                    cpf = user.Colportor.CPF,
                    city = user.Colportor.City,
                    gender = user.Colportor.Gender,
                    birthDate = user.Colportor.BirthDate,
                    lastVisitDate = user.Colportor.LastVisitDate,
                    photoUrl = user.Colportor.PhotoUrl,
                    regionId = user.Colportor.RegionId,
                    leaderId = user.Colportor.LeaderId,
                    leaderName = user.Colportor.Leader?.FullName ?? user.Colportor.Leader?.Email,
                    dueDate = user.Colportor.LastVisitDate?.AddYears(1) // Data de vencimento = última visita + 1 ano
                } : null,
                // Dados da região se existir
                region = user.Region != null ? new
                {
                    id = user.Region.Id,
                    name = user.Region.Name
                } : null
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao obter dados do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza dados do usuário logado (para wallet)
    /// </summary>
    [HttpPut("wallet/me")]
    [Route("wallet/me")]
    [Route("/wallet/me")]
    [Authorize]
    public async Task<IActionResult> UpdateWalletUser([FromBody] UpdateWalletUserDto updateDto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Atualizar dados do usuário
            if (!string.IsNullOrEmpty(updateDto.FullName))
                user.FullName = updateDto.FullName;
            if (!string.IsNullOrEmpty(updateDto.Email))
                user.Email = updateDto.Email;
            if (!string.IsNullOrEmpty(updateDto.CPF))
                user.CPF = updateDto.CPF;
            if (!string.IsNullOrEmpty(updateDto.City))
                user.City = updateDto.City;
            if (updateDto.RegionId.HasValue)
                user.RegionId = updateDto.RegionId.Value;

            // Atualizar senha se fornecida
            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.Password);
            }

            // Atualizar dados do colportor se existir
            if (user.Colportor != null)
            {
                if (!string.IsNullOrEmpty(updateDto.FullName))
                    user.Colportor.FullName = updateDto.FullName;
                if (!string.IsNullOrEmpty(updateDto.CPF))
                    user.Colportor.CPF = updateDto.CPF;
                if (!string.IsNullOrEmpty(updateDto.Gender))
                    user.Colportor.Gender = updateDto.Gender;
                if (updateDto.BirthDate.HasValue)
                    user.Colportor.BirthDate = updateDto.BirthDate.Value;
                if (!string.IsNullOrEmpty(updateDto.City))
                    user.Colportor.City = updateDto.City;
                if (!string.IsNullOrEmpty(updateDto.PhotoUrl))
                    user.Colportor.PhotoUrl = updateDto.PhotoUrl;
                if (updateDto.RegionId.HasValue)
                    user.Colportor.RegionId = updateDto.RegionId.Value;
                if (updateDto.LeaderId.HasValue)
                    user.Colportor.LeaderId = updateDto.LeaderId.Value;
            }

            await _authService.UpdateUserAsync(user);

            Logger.LogInformation("Dados do usuário {UserId} atualizados com sucesso", userId);
            return Ok(new { message = "Dados atualizados com sucesso" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar dados do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Calcula o status do colportor baseado na última visita
    /// </summary>
    private static string CalculateStatus(DateTime? lastVisitDate)
    {
        if (!lastVisitDate.HasValue)
        {
            return "PENDENTE";
        }

        var dueDate = lastVisitDate.Value.AddYears(1);
        var today = DateTime.UtcNow.Date;
        var daysUntilDue = (dueDate.Date - today).TotalDays;

        if (daysUntilDue < 0)
        {
            return "VENCIDO";
        }
        else if (daysUntilDue <= 30)
        {
            return "AVISO";
        }
        else
        {
            return "EM DIA";
        }
    }

}

/// <summary>
/// DTO para atualização de dados do usuário na carteira
/// </summary>
public class UpdateWalletUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? CPF { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? City { get; set; }
    public string? PhotoUrl { get; set; }
    public int? RegionId { get; set; }
    public int? LeaderId { get; set; }
    public string? Password { get; set; }
}