using Colportor.Api.DTOs;
using Colportor.Api.Models;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviços de autenticação
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Realiza login do usuário
    /// </summary>
    Task<ApiResponse<AuthResultDto>> LoginAsync(string email, string password);

    /// <summary>
    /// Registra um novo usuário (líder)
    /// </summary>
    Task<ApiResponse<AuthResultDto>> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Registra um novo colportor
    /// </summary>
    Task<ApiResponse<AuthResultDto>> RegisterColportorAsync(RegisterDto registerDto);

    /// <summary>
    /// Atualiza o token JWT
    /// </summary>
    Task<ApiResponse<AuthResultDto>> RefreshTokenAsync(int userId);

    /// <summary>
    /// Obtém usuário por ID
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Obtém usuário por email
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Valida se a senha está correta
    /// </summary>
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);

    /// <summary>
    /// Gera hash da senha
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Lista usuários pendentes de aprovação
    /// </summary>
    Task<IEnumerable<UserDto>> GetPendingUsersAsync();

    /// <summary>
    /// Aprova um usuário pendente
    /// </summary>
    Task<ApiResponse<bool>> ApproveUserAsync(int userId);

    /// <summary>
    /// Lista líderes por região
    /// </summary>
    Task<IEnumerable<UserDto>> GetLeadersByRegionAsync(int regionId);

    /// <summary>
    /// Lista todos os líderes (Admin)
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllLeadersAsync();
}

/// <summary>
/// DTO para resultado de autenticação
/// </summary>
public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

/// <summary>
/// DTO para dados do usuário
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? CPF { get; set; }
    public string? City { get; set; }
    public int? RegionId { get; set; }
    public string? RegionName { get; set; }
    public int? ColportorId { get; set; }
}
