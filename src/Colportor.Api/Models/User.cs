// src/Colportor.Api/Models/User.cs
namespace Colportor.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        // Autentica��o
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = "Colportor"; // "Admin" | "Leader" | "Colportor"

        // Dados pessoais (para líderes)
        public string? FullName { get; set; }
        public string? CPF { get; set; }
        public string? City { get; set; }

        // Se for um colportor, aponta para o registro dele
        public int? ColportorId { get; set; }
        public Colportor? Colportor { get; set; }

        // Se for L�DER, restringe pela regi�o
        public int? RegionId { get; set; }       
        public Region? Region { get; set; }          // navega��o opcional
    }
}
