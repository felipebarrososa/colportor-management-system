using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Colportor.Api.Models;

namespace Colportor.Api.Services;
public class JwtService
{
    private readonly string _key;
    public JwtService(IConfiguration cfg)
    {
        _key = cfg["JWT_KEY"] ?? cfg["Jwt:Key"] ?? "dev_key_change_me";
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
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });
        return handler.WriteToken(token);
    }
}
