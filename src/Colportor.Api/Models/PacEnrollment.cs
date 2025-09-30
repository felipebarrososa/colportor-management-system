using System;

namespace Colportor.Api.Models
{
    public class PacEnrollment
    {
        public int Id { get; set; }

        public int LeaderId { get; set; }          // User.Id (Role Leader)
        public User Leader { get; set; } = default!;

        public int ColportorId { get; set; }
        public Colportor Colportor { get; set; } = default!;

        /// <summary>UTC (data inclusive)</summary>
        public DateTime StartDate { get; set; }
        /// <summary>UTC (data inclusive)</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Pending | Approved | Rejected</summary>
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


