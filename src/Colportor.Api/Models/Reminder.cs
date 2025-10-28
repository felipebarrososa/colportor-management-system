using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colportor.Api.Models
{
    public class Reminder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }

        [ForeignKey("ContactId")]
        public virtual MissionContact Contact { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "medium"; // low, medium, high

        [Required]
        public bool Completed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [StringLength(50)]
        public string? CreatedBy { get; set; } // Email do usuário que criou
    }
}

