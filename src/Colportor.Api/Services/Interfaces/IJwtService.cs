using Colportor.Api.Models;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviço JWT
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gera token JWT para o usuário
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Valida token JWT
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Extrai claims do token
    /// </summary>
    Dictionary<string, string> ExtractClaims(string token);
}
