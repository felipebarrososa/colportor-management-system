using Colportor.Api.DTOs;
using Colportor.Api.Models;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviços de colportores
/// </summary>
public interface IColportorService
{
    /// <summary>
    /// Lista colportores com paginação
    /// </summary>
    Task<PagedResult<ColportorDto>> GetColportorsAsync(int userId, string userRole, int page = 1, int pageSize = 10);

    /// <summary>
    /// Obtém colportor por ID
    /// </summary>
    Task<ColportorDto?> GetColportorByIdAsync(int id, int userId, string userRole);

    /// <summary>
    /// Cria novo colportor
    /// </summary>
    Task<ApiResponse<ColportorDto>> CreateColportorAsync(CreateColportorDto createDto, int userId, string userRole);

    /// <summary>
    /// Atualiza colportor
    /// </summary>
    Task<ApiResponse<ColportorDto>> UpdateColportorAsync(int id, UpdateColportorDto updateDto, int userId, string userRole);

    /// <summary>
    /// Deleta colportor
    /// </summary>
    Task<ApiResponse> DeleteColportorAsync(int id);

    /// <summary>
    /// Obtém estatísticas de colportores
    /// </summary>
    Task<ColportorStatsDto> GetColportorStatsAsync();
}

/// <summary>
/// DTO para dados do colportor
/// </summary>
public class ColportorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? RegionId { get; set; }
    public string? RegionName { get; set; }
    public string? City { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VisitDto> Visits { get; set; } = new();
    public List<PacEnrollmentDto> PacEnrollments { get; set; } = new();
}

/// <summary>
/// DTO para visita
/// </summary>
public class VisitDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para inscrição PAC
/// </summary>
public class PacEnrollmentDto
{
    public int Id { get; set; }
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para estatísticas de colportores
/// </summary>
public class ColportorStatsDto
{
    public int TotalColportors { get; set; }
    public int ActiveColportors { get; set; }
    public int MaleColportors { get; set; }
    public int FemaleColportors { get; set; }
    public Dictionary<string, int> ColportorsByRegion { get; set; } = new();
    public Dictionary<string, int> ColportorsByStatus { get; set; } = new();
}

/// <summary>
/// DTO para resultado paginado
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
