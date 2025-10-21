namespace Colportor.Api.DTOs
{
    public class ContactObservationCreateDto
    {
        public int MissionContactId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }

    public class ContactObservationDto
    {
        public int Id { get; set; }
        public int MissionContactId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

