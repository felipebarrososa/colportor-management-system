namespace Colportor.Api.DTOs
{
    public class UpdateColportorDto
    {
        public string? FullName { get; set; }
        public string? CPF { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? City { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int? RegionId { get; set; }
        public int? LeaderId { get; set; }
    }
}
