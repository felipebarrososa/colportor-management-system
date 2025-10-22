using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.Services.Interfaces;
using Colportor.Api.DTOs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text;
using System.Threading;

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
        public async Task<IActionResult> GetMessages(string phoneNumber, [FromQuery] int limit = 500, [FromQuery] bool includeMedia = true)
        {
            try
            {
                var result = await _whatsAppService.GetMessagesAsync(phoneNumber, GetCurrentUserId(), GetCurrentUserRole(), limit, includeMedia);
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

        [HttpGet("message-media/{messageId}")]
        public async Task<IActionResult> GetMessageMedia(string messageId)
        {
            try
            {
                var http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("WHATSAPP_SERVICE_URL") ?? "http://whatsapp:3001") };
                var resp = await http.GetAsync($"/message-media/{messageId}");
                var body = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, body);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter mídia da mensagem WhatsApp");
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

        [HttpGet("messages/stream/{phoneNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> StreamMessages(string phoneNumber)
        {
            try
            {
                Response.Headers.Append("Content-Type", "text/event-stream");
                Response.Headers.Append("Cache-Control", "no-cache");
                Response.Headers.Append("Connection", "keep-alive");
                Response.Headers.Append("Access-Control-Allow-Origin", "*");
                Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

                var lastMessageCount = 0;
                var lastCheck = DateTime.UtcNow;

                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    try
                    {
                        // Para SSE, usar um usuário padrão ou buscar diretamente do banco
                        var messagesResult = await _whatsAppService.GetMessagesAsync(phoneNumber, 1, "Admin", 500, true);
                        var messages = messagesResult.Data ?? Enumerable.Empty<WhatsAppMessageResponseDto>();
                        var currentCount = messages.Count();

                        // Se o número de mensagens mudou, enviar atualização
                        if (currentCount != lastMessageCount)
                        {
                            var data = new
                            {
                                type = "messages_update",
                                phoneNumber = phoneNumber,
                                messageCount = currentCount,
                                timestamp = DateTime.UtcNow,
                                messages = messages.OrderByDescending(m => m.Timestamp).Take(10).Select(m => new
                                {
                                    id = m.Id,
                                    content = m.Content,
                                    sender = m.Sender,
                                    timestamp = m.Timestamp,
                                    mediaUrl = m.MediaUrl,
                                    mediaType = m.MediaType,
                                    hasMedia = !string.IsNullOrEmpty(m.MediaUrl)
                                })
                            };

                            var json = System.Text.Json.JsonSerializer.Serialize(data);
                            var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
                            
                            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                            await Response.Body.FlushAsync();

                            lastMessageCount = currentCount;
                        }

                        lastCheck = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Erro ao verificar mensagens para SSE");
                        
                        var errorData = new
                        {
                            type = "error",
                            message = "Erro ao verificar mensagens",
                            timestamp = DateTime.UtcNow
                        };

                        var errorJson = System.Text.Json.JsonSerializer.Serialize(errorData);
                        var errorBytes = Encoding.UTF8.GetBytes($"data: {errorJson}\n\n");
                        
                        await Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
                        await Response.Body.FlushAsync();
                    }

                    // Aguardar 1 segundo antes da próxima verificação
                    await Task.Delay(1000, HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Cliente desconectou, encerrar stream
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro no stream de mensagens");
            }

            return new EmptyResult();
        }
    }
}
