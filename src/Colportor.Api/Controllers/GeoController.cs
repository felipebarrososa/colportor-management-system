using Microsoft.AspNetCore.Mvc;
using Colportor.Api.Services;
using Colportor.Api.Data;

namespace Colportor.Api.Controllers;

/// <summary>
/// Controller para endpoints geográficos públicos
/// </summary>
[ApiController]
[Route("[controller]")]
[Route("")]
public class GeoController : BaseController
{
    private readonly IRegionService _regionService;
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public GeoController(IRegionService regionService, IAuthService authService, AppDbContext context, ILogger<GeoController> logger) 
        : base(logger)
    {
        _regionService = regionService;
        _authService = authService;
        _context = context;
    }

    /// <summary>
    /// Lista todos os países (público)
    /// </summary>
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries()
    {
        try
        {
            var countries = await _regionService.GetCountriesAsync();
            return Ok(countries);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar países");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista regiões por país (público)
    /// </summary>
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegionsByCountry([FromQuery] int countryId)
    {
        try
        {
            var regions = await _regionService.GetRegionsByCountryAsync(countryId);
            return Ok(regions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar regiões por país {CountryId}", countryId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista líderes por região (público)
    /// </summary>
    [HttpGet("leaders")]
    public async Task<IActionResult> GetLeadersByRegion([FromQuery] int regionId)
    {
        try
        {
            var leaders = await _authService.GetLeadersByRegionAsync(regionId);
            return Ok(leaders);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao listar líderes por região {RegionId}", regionId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Upload de foto (público) - salva no banco de dados
    /// </summary>
    [HttpPost("upload/photo")]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile photo, [FromForm] string? colportorId = null)
    {
        try
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new { message = "Nenhum arquivo enviado" });
            }

            // Validar tipo de arquivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Tipo de arquivo não permitido. Use JPG, PNG ou GIF." });
            }

            // Validar tamanho (máximo 5MB)
            if (photo.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Arquivo muito grande. Máximo 5MB." });
            }

            // Ler dados do arquivo
            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                await photo.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }

            // Converter colportorId de string para int se fornecido
            int? colportorIdInt = null;
            if (!string.IsNullOrEmpty(colportorId) && int.TryParse(colportorId, out var parsedId))
            {
                colportorIdInt = parsedId;
            }

            // Criar registro da foto no banco
            var photoRecord = new Colportor.Api.Models.Photo
            {
                FileName = photo.FileName,
                ContentType = photo.ContentType,
                Data = fileData,
                CreatedAt = DateTime.UtcNow,
                ColportorId = colportorIdInt
            };

            // Salvar no banco de dados
            _context.Photos.Add(photoRecord);
            await _context.SaveChangesAsync();

            // Retornar ID da foto para o frontend
            Logger.LogInformation("Foto salva no banco com sucesso: ID {PhotoId}, Nome: {FileName}, ColportorId: {ColportorId}", 
                photoRecord.Id, photoRecord.FileName, colportorIdInt);
            return Ok(new { url = $"/photo/{photoRecord.Id}" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao fazer upload da foto");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Serve foto do banco de dados (público)
    /// </summary>
    [HttpGet("photo/{id}")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound(new { message = "Foto não encontrada" });
            }

            return File(photo.Data, photo.ContentType, photo.FileName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao servir foto {PhotoId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}
