namespace Colportor.Api.DTOs;

public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? CPF { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? City { get; set; }
    public string? PhotoUrl { get; set; }
    public int? RegionId { get; set; }
    public int? LeaderId { get; set; }
    public DateTime? LastVisitDate { get; set; }
}

