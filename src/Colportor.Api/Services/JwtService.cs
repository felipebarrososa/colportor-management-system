using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Colportor.Api.Models;

namespace Colportor.Api.Services;
public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    
    public JwtService(IConfiguration cfg)
    {
        _key = cfg["JWT_KEY"] ?? cfg["Jwt:Key"] ?? "dev_key_change_me";
        _issuer = cfg["Jwt:Issuer"] ?? "ColportorAPI";
        _audience = cfg["Jwt:Audience"] ?? "ColportorClient";
        Console.WriteLine($"JwtService initialized with key length: {_key.Length}");
    }
    public string GenerateToken(User user)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Issuer = _issuer,
            Audience = _audience,
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });
        return handler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_key);
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, string> ExtractClaims(string token)
    {
        var claims = new Dictionary<string, string>();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            foreach (var claim in jsonToken.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
        }
        catch
        {
            // Retorna dicionário vazio em caso de erro
        }
        return claims;
    }
}
