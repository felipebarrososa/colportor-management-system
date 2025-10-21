using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.Models
{
    public class ContactObservation
    {
        public int Id { get; set; }
        
        [Required]
        public int MissionContactId { get; set; }
        public MissionContact? MissionContact { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
