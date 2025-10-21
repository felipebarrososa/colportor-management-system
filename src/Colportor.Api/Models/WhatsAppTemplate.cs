using System;
using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.Models
{
    public class WhatsAppTemplate
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Category { get; set; } = "General";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        
        // Variáveis disponíveis no template
        [MaxLength(500)]
        public string? AvailableVariables { get; set; } // JSON com variáveis como {nome}, {telefone}, etc.
    }
}

