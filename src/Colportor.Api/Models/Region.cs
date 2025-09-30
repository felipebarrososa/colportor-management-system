using System.Collections.Generic;

namespace Colportor.Api.Models
{
    public class Region
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;

        public int CountryId { get; set; }
        public Country Country { get; set; } = default!;

        public ICollection<Colportor> Colportors { get; set; } = new List<Colportor>();

        // usuários com papel Leader associados a esta região
        public ICollection<User> Leaders { get; set; } = new List<User>();
    }
}
