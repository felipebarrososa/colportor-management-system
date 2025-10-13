using Microsoft.AspNetCore.Mvc;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller base com funcionalidades comuns
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected readonly ILogger Logger;

    protected BaseController(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Retorna o ID do usuário logado
    /// </summary>
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Retorna o role do usuário logado
    /// </summary>
    protected string GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
        return roleClaim?.Value ?? string.Empty;
    }

    /// <summary>
    /// Retorna o email do usuário logado
    /// </summary>
    protected string GetCurrentUserEmail()
    {
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email);
        return emailClaim?.Value ?? string.Empty;
    }

    /// <summary>
    /// Verifica se o usuário tem o role especificado
    /// </summary>
    protected bool HasRole(string role)
    {
        return GetCurrentUserRole().Equals(role, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se o usuário é admin
    /// </summary>
    protected bool IsAdmin()
    {
        return HasRole("Admin");
    }

    /// <summary>
    /// Verifica se o usuário é líder
    /// </summary>
    protected bool IsLeader()
    {
        return HasRole("Leader");
    }

    /// <summary>
    /// Verifica se o usuário é colportor
    /// </summary>
    protected bool IsColportor()
    {
        return HasRole("Colportor");
    }
}
