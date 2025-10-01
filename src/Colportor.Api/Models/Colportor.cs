using System;
using System.Collections.Generic;

namespace Colportor.Api.Models
{
    public class Colportor
    {
        public int Id { get; set; }

        public string FullName { get; set; } = default!;
        public string CPF { get; set; } = default!;

        // Agora Pa�s/Regi�o s�o relacionais (IDs), n�o strings:
        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public int? RegionId { get; set; }
        public Region? Region { get; set; }

        public string? City { get; set; }
        public string? PhotoUrl { get; set; }

        /// <summary>UTC</summary>
        public DateTime? LastVisitDate { get; set; }

        /// <summary>L�der respons�vel (User.Id) � opcional</summary>
        public int? LeaderId { get; set; }
        public User? Leader { get; set; }

        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<PacEnrollment> PacEnrollments { get; set; } = new List<PacEnrollment>();
    }
}
