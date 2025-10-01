public record LeaderRegisterDto(
    string FullName,
    string CPF,
    string? City,
    string Email,
    string Password,
    int RegionId
);
