using System;

namespace Colportor.Api.Models
{
    public class Visit
    {
        public int Id { get; set; }

        public int ColportorId { get; set; }
        public Colportor Colportor { get; set; } = default!;

        /// <summary>UTC (mapeado para timestamptz)</summary>
        public DateTime Date { get; set; }
    }
}
