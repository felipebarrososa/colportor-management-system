using Colportor.Api.DTOs;

namespace Colportor.Api.Services;

/// <summary>
/// Interface para serviço de paginação
/// </summary>
public interface IPaginationService
{
    /// <summary>
    /// Cria resultado paginado
    /// </summary>
    PagedResult<T> CreatePagedResult<T>(IEnumerable<T> items, int totalCount, int page, int pageSize);

    /// <summary>
    /// Valida parâmetros de paginação
    /// </summary>
    (int page, int pageSize) ValidatePaginationParams(int page, int pageSize, int maxPageSize = 100);

    /// <summary>
    /// Calcula offset para paginação
    /// </summary>
    int CalculateOffset(int page, int pageSize);

    /// <summary>
    /// Calcula total de páginas
    /// </summary>
    int CalculateTotalPages(int totalCount, int pageSize);
}

/// <summary>
/// Implementação do serviço de paginação
/// </summary>
public class PaginationService : IPaginationService
{
    private readonly ILogger<PaginationService> _logger;

    public PaginationService(ILogger<PaginationService> logger)
    {
        _logger = logger;
    }

    public PagedResult<T> CreatePagedResult<T>(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        var itemsList = items.ToList();
        var totalPages = CalculateTotalPages(totalCount, pageSize);

        return new PagedResult<T>
        {
            Items = itemsList,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public (int page, int pageSize) ValidatePaginationParams(int page, int pageSize, int maxPageSize = 100)
    {
        // Validar página
        if (page < 1)
        {
            _logger.LogWarning("Página inválida: {Page}. Usando página 1.", page);
            page = 1;
        }

        // Validar tamanho da página
        if (pageSize < 1)
        {
            _logger.LogWarning("Tamanho da página inválido: {PageSize}. Usando 10.", pageSize);
            pageSize = 10;
        }
        else if (pageSize > maxPageSize)
        {
            _logger.LogWarning("Tamanho da página muito grande: {PageSize}. Limitando para {MaxPageSize}.", pageSize, maxPageSize);
            pageSize = maxPageSize;
        }

        return (page, pageSize);
    }

    public int CalculateOffset(int page, int pageSize)
    {
        return (page - 1) * pageSize;
    }

    public int CalculateTotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling((double)totalCount / pageSize);
    }
}
