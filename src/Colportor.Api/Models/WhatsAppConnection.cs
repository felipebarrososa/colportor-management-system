using System;
using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.Models
{
    public class WhatsAppConnection
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Disconnected"; // Disconnected, Connecting, Connected, Error
        
        public DateTime? LastConnection { get; set; }
        
        public DateTime? LastDisconnection { get; set; }
        
        [MaxLength(500)]
        public string? QrCode { get; set; }
        
        public DateTime? QrCodeGeneratedAt { get; set; }
        
        public DateTime? QrCodeExpiresAt { get; set; }
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
        
        [MaxLength(100)]
        public string? SessionData { get; set; } // Dados da sessão do WhatsApp
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}

