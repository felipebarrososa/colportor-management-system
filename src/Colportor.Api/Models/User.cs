// src/Colportor.Api/Models/User.cs
namespace Colportor.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        // Autenticação
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = "Colportor"; // "Admin" | "Leader" | "Colportor"

        // Se for um colportor, aponta para o registro dele
        public int? ColportorId { get; set; }
        public Colportor? Colportor { get; set; }

        // Se for LÍDER, restringe pela região
        public int? RegionId { get; set; }       
        public Region? Region { get; set; }          // navegação opcional
    }
}
