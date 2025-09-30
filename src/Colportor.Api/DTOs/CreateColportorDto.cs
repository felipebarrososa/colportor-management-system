namespace Colportor.Api.DTOs
{
    public class CreateColportorDto
    {
    public string FullName { get; set; } = "";
    public string CPF { get; set; } = "";
    public string? Country { get; set; } = "Brasil";
    public string? City { get; set; }
    public string? PhotoUrl { get; set; }

    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    public int RegionId { get; set; }             // novo (obrigatório)
    public int? LeaderId { get; set; }            // novo (opcional)

    // opcional: última visita informada no cadastro (UTC ou sem timezone)
    public DateTime? LastVisitDate { get; set; }
    }
}
