using Colportor.Api.Data;
using Colportor.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Colportor.Api.Services;

public class SeedDataService
{
    private readonly AppDbContext _context;

    public SeedDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Verificar se já existe usuário admin
        var adminExists = await _context.Users.AnyAsync(u => u.Email == "admin@colportor.local");
        
        if (!adminExists)
        {
            // Criar usuário admin
            var adminUser = new User
            {
                Email = "admin@colportor.local",
                FullName = "Administrador do Sistema",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                CPF = "00000000000",
                City = "São Paulo"
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("✅ Usuário admin criado com sucesso!");
            Console.WriteLine("📧 Email: admin@colportor.local");
            Console.WriteLine("🔑 Senha: admin123");
        }
        else
        {
            Console.WriteLine("ℹ️ Usuário admin já existe no banco de dados.");
        }
    }
}
