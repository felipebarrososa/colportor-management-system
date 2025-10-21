using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services.Interfaces;
using Colportor.Api.DTOs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Colportor.Api.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    [Authorize]
    public class WhatsAppController : BaseController
    {
        private readonly IWhatsAppService _whatsAppService;

        public WhatsAppController(IWhatsAppService whatsAppService, ILogger<WhatsAppController> logger)
            : base(logger)
        {
            _whatsAppService = whatsAppService;
        }

        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var result = await _whatsAppService.GetConnectionStatusAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter status do WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("connect")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Connect()
        {
            try
            {
                var result = await _whatsAppService.ConnectAsync();
                if (result.Success)
                    return Ok(result.Data);
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao conectar WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("disconnect")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Disconnect()
        {
            try
            {
                var result = await _whatsAppService.DisconnectAsync();
                if (result.Success)
                    return Ok(result.Data);
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao desconectar WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var result = await _whatsAppService.GetStatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter estatísticas do WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin,Leader")]
        public async Task<IActionResult> SendMessage([FromBody] WhatsAppMessageDto dto)
        {
            try
            {
                var result = await _whatsAppService.SendMessageAsync(dto, GetCurrentUserId(), GetCurrentUserRole());
                if (result.Success)
                    return Ok(result.Data);
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao enviar mensagem WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("messages/{phoneNumber}")]
        public async Task<IActionResult> GetMessages(string phoneNumber)
        {
            try
            {
                var result = await _whatsAppService.GetMessagesAsync(phoneNumber, GetCurrentUserId(), GetCurrentUserRole());
                if (result.Success)
                    return Ok(result.Data);
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter mensagens do WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("send-media")]
        [Authorize(Roles = "Admin,Leader")]
        public async Task<IActionResult> SendMedia()
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null)
                    return BadRequest(new { message = "Nenhum arquivo enviado" });

                var contactId = Request.Form["contactId"].FirstOrDefault();
                var phoneNumber = Request.Form["phoneNumber"].FirstOrDefault();

                if (string.IsNullOrEmpty(contactId) || string.IsNullOrEmpty(phoneNumber))
                    return BadRequest(new { message = "ContactId e PhoneNumber são obrigatórios" });

                // Criar DTO para envio de mídia
                var dto = new WhatsAppMessageDto
                {
                    ContactId = int.Parse(contactId),
                    PhoneNumber = phoneNumber,
                    Content = file.FileName,
                    MediaType = file.ContentType.Split('/')[0], // image, video, audio
                    MediaFile = file
                };

                var result = await _whatsAppService.SendMessageAsync(dto, GetCurrentUserId(), GetCurrentUserRole());
                if (result.Success)
                    return Ok(result.Data);
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao enviar mídia via WhatsApp");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
