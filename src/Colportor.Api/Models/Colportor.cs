using System;
using System.Collections.Generic;

namespace Colportor.Api.Models
{
    public class Colportor
    {
        public int Id { get; set; }

        public string FullName { get; set; } = default!;
        public string CPF { get; set; } = default!;

        // Agora País/Região são relacionais (IDs), não strings:
        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public int? RegionId { get; set; }
        public Region? Region { get; set; }

        public string? City { get; set; }
        public string? PhotoUrl { get; set; }

        /// <summary>UTC</summary>
        public DateTime? LastVisitDate { get; set; }

        /// <summary>Líder responsável (User.Id) — opcional</summary>
        public int? LeaderId { get; set; }
        public User? Leader { get; set; }

        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}
