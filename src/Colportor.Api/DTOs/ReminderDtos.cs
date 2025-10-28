using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.DTOs
{
    public class CreateReminderDto
    {
        [Required]
        public int ContactId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres")]
        public string? Description { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "medium";
    }

    public class UpdateReminderDto
    {
        [Required]
        public int Id { get; set; }

        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres")]
        public string? Description { get; set; }

        public DateTime? DateTime { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; }

        public bool? Completed { get; set; }
    }

    public class ReminderResponseDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DateTime { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class ReminderSummaryDto
    {
        public int Total { get; set; }
        public int Overdue { get; set; }
        public int Today { get; set; }
        public int Future { get; set; }
    }
}
