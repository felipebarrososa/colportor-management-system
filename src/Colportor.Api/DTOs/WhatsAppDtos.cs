using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Colportor.Api.DTOs
{
    public class WhatsAppConnectionStatusDto
    {
        public bool Connected { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? LastConnection { get; set; }
        public string? QrCode { get; set; }
        public string Status { get; set; } = "Disconnected";
    }

    public class WhatsAppStatsDto
    {
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public int MessagesRead { get; set; }
        public int MessagesFailed { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class WhatsAppMessageDto
    {
        [Required]
        public int ContactId { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        public IFormFile? MediaFile { get; set; }
    }

    public class WhatsAppMessageResponseDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
    }

    public class WhatsAppServiceMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public object Status { get; set; } = string.Empty; // Pode ser string ou int
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
    }

    public class WhatsAppTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WhatsAppTemplateCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Category { get; set; } = "General";
    }
}
