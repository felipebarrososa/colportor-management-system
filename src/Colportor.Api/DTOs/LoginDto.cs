using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.DTOs;

/// <summary>
/// DTO para login de usuário
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;
}
