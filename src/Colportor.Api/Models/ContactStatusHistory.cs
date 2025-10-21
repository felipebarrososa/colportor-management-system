using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.Models
{
    public class ContactStatusHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int MissionContactId { get; set; }
        public MissionContact? MissionContact { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string FromStatus { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string ToStatus { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ChangedBy { get; set; }
        
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

