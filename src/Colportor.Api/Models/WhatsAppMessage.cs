using System;
using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.Models
{
    public class WhatsAppMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int MissionContactId { get; set; }
        public MissionContact? MissionContact { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Sender { get; set; } = string.Empty; // 'user' ou 'contact'
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "sent"; // sent, delivered, read, failed
        
        public string? MediaUrl { get; set; }
        
        [MaxLength(50)]
        public string? MediaType { get; set; } // image, document, audio, video
        
        public string? WhatsAppMessageId { get; set; } // ID da mensagem no WhatsApp
        
        public int? SentByUserId { get; set; }
        public User? SentByUser { get; set; }
    }
}

