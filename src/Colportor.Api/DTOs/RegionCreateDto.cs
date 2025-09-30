public record RegisterDto(
    string FullName,
    string CPF,
    string? Country,   // opcional (legacy); vamos usar RegionId
    string? Region,    // opcional (legacy)
    string? City,
    string? PhotoUrl,
    string Email,
    string Password,
    int? RegionId,     // <<< NOVO
    int? LeaderId      // <<< NOVO (opcional)
);
