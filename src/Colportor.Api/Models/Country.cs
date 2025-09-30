using System.Collections.Generic;

namespace Colportor.Api.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;

        /// <summary>Sigla opcional (ex.: BR, US)</summary>
        public string? Iso2 { get; set; }

        public ICollection<Region> Regions { get; set; } = new List<Region>();
        public ICollection<Colportor> Colportors { get; set; } = new List<Colportor>();
    }
}
