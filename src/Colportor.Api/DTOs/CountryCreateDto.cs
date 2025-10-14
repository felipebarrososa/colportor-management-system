namespace Colportor.Api.DTOs;

/// <summary>
/// DTO para criação de país
/// </summary>
public class CountryCreateDto
{
    /// <summary>
    /// Nome do país
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Código do país (ex: BR, AR, CL)
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
