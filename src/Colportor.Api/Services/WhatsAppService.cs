using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories.Interfaces;
using Colportor.Api.Repositories;
using Colportor.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace Colportor.Api.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private class WhatsAppServiceMessagesEnvelope
        {
            public bool Success { get; set; }
            public List<WhatsAppServiceMessageDto>? Messages { get; set; }
        }
        private readonly IMapper _mapper;
        private readonly IWhatsAppMessageRepository _messageRepo;
        private readonly IWhatsAppTemplateRepository _templateRepo;
        private readonly IWhatsAppConnectionRepository _connectionRepo;
        private readonly IMissionContactRepository _contactRepo;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _whatsappServiceUrl;

        public WhatsAppService(
            IMapper mapper, 
            IWhatsAppMessageRepository messageRepo,
            IWhatsAppTemplateRepository templateRepo,
            IWhatsAppConnectionRepository connectionRepo,
            IMissionContactRepository contactRepo,
            ILogger<WhatsAppService> logger,
            HttpClient httpClient)
        {
            _mapper = mapper;
            _messageRepo = messageRepo;
            _templateRepo = templateRepo;
            _connectionRepo = connectionRepo;
            _contactRepo = contactRepo;
            _logger = logger;
            _httpClient = httpClient;
            _whatsappServiceUrl = Environment.GetEnvironmentVariable("WHATSAPP_SERVICE_URL") ?? "http://whatsapp:3001";
        }

        public async Task<WhatsAppConnectionStatusDto> GetConnectionStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_whatsappServiceUrl}/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<WhatsAppConnectionStatusDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return status ?? new WhatsAppConnectionStatusDto { Connected = false, Status = "Error" };
                }
                else
                {
                    _logger.LogWarning("Erro ao obter status do WhatsApp Service: {StatusCode}", response.StatusCode);
                    return new WhatsAppConnectionStatusDto { Connected = false, Status = "ServiceUnavailable" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status da conexão WhatsApp");
                return new WhatsAppConnectionStatusDto
                {
                    Connected = false,
                    Status = "Error"
                };
            }
        }

        public async Task<ApiResponse<WhatsAppConnectionStatusDto>> ConnectAsync()
        {
            try
            {
                _logger.LogInformation("Tentando conectar WhatsApp - URL: {Url}", _whatsappServiceUrl);
                
                var response = await _httpClient.PostAsync($"{_whatsappServiceUrl}/connect", null);
                _logger.LogInformation("Resposta do WhatsApp Service - Status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Resposta JSON do WhatsApp: {Json}", json);
                    
                    var status = JsonSerializer.Deserialize<WhatsAppConnectionStatusDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (status != null)
                    {
                        _logger.LogInformation("WhatsApp conectado com sucesso - Status: {Status}", status.Status);
                        return ApiResponse<WhatsAppConnectionStatusDto>.SuccessResponse(status);
                    }
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Erro ao conectar WhatsApp - Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                
                return ApiResponse<WhatsAppConnectionStatusDto>.ErrorResponse($"WhatsApp service não está disponível. Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar WhatsApp - URL: {Url}", _whatsappServiceUrl);
                return ApiResponse<WhatsAppConnectionStatusDto>.ErrorResponse("WhatsApp service não está rodando. Verifique se o serviço foi iniciado.");
            }
        }

        public async Task<ApiResponse> DisconnectAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_whatsappServiceUrl}/disconnect", null);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp desconectado");
                    return ApiResponse.SuccessResponse("WhatsApp desconectado com sucesso");
                }
                
                return ApiResponse.ErrorResponse("Erro ao desconectar WhatsApp Service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desconectar WhatsApp");
                return ApiResponse.ErrorResponse("Erro interno ao desconectar WhatsApp");
            }
        }

        public async Task<WhatsAppStatsDto> GetStatsAsync()
        {
            try
            {
                var messages = await _messageRepo.GetAllAsync();
                
                return new WhatsAppStatsDto
                {
                    MessagesSent = messages.Count(m => m.Sender == "user"),
                    MessagesDelivered = messages.Count(m => m.Status == "delivered"),
                    MessagesRead = messages.Count(m => m.Status == "read"),
                    MessagesFailed = messages.Count(m => m.Status == "failed"),
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do WhatsApp");
                return new WhatsAppStatsDto();
            }
        }

        public async Task<ApiResponse<WhatsAppMessageResponseDto>> SendMessageAsync(WhatsAppMessageDto dto, int currentUserId, string currentUserRole)
        {
            try
            {
                _logger.LogInformation("Enviando mensagem WhatsApp - ContactId: {ContactId}, Content: {Content}", 
                    dto.ContactId, dto.Content);
                
                // Verificar se o contato existe
                var contact = await _contactRepo.GetByIdAsync(dto.ContactId);
                if (contact == null)
                {
                    _logger.LogWarning("Contato não encontrado: {ContactId}", dto.ContactId);
                    return ApiResponse<WhatsAppMessageResponseDto>.ErrorResponse("Contato não encontrado");
                }
                
                _logger.LogInformation("Contato encontrado - Phone: {Phone}", contact.Phone);

                HttpResponseMessage response;
                
                // Se tem arquivo de mídia, enviar via multipart/form-data
                if (dto.MediaFile != null)
                {
                    _logger.LogInformation("Enviando arquivo de mídia: {FileName}, Tipo: {ContentType}", 
                        dto.MediaFile.FileName, dto.MediaFile.ContentType);
                    
                    using var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(dto.PhoneNumber), "phoneNumber");
                    formData.Add(new StringContent(dto.Content), "message");
                    formData.Add(new StringContent(dto.MediaType ?? ""), "mediaType");
                    formData.Add(new StreamContent(dto.MediaFile.OpenReadStream()), "media", dto.MediaFile.FileName);
                    
                    response = await _httpClient.PostAsync($"{_whatsappServiceUrl}/send-media", formData);
                }
                else
                {
                    // Enviar mensagem de texto via JSON
                    var payload = new
                    {
                        phoneNumber = dto.PhoneNumber,
                        message = dto.Content,
                        mediaUrl = dto.MediaUrl,
                        mediaType = dto.MediaType
                    };

                    var json = JsonSerializer.Serialize(payload);
                    _logger.LogInformation("Payload enviado para WhatsApp Service: {Payload}", json);
                    
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    response = await _httpClient.PostAsync($"{_whatsappServiceUrl}/send-message", content);
                }
                
                _logger.LogInformation("Resposta do WhatsApp Service: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    // Criar mensagem no banco
                    var message = new WhatsAppMessage
                    {
                        MissionContactId = dto.ContactId,
                        Content = dto.Content,
                        Sender = "user",
                        Timestamp = DateTime.UtcNow,
                        Status = "sent",
                        MediaUrl = dto.MediaUrl,
                        MediaType = dto.MediaType,
                        SentByUserId = currentUserId,
                        WhatsAppMessageId = result.TryGetProperty("messageId", out var msgId) ? msgId.GetString() : null
                    };

                    await _messageRepo.AddAsync(message);

                    var responseDto = _mapper.Map<WhatsAppMessageResponseDto>(message);
                    responseDto.ContactId = dto.ContactId;

                    _logger.LogInformation("Mensagem WhatsApp enviada para contato {ContactId}: {Content}", dto.ContactId, dto.Content);
                    
                    return ApiResponse<WhatsAppMessageResponseDto>.SuccessResponse(responseDto);
                }
                else
                {
                    return ApiResponse<WhatsAppMessageResponseDto>.ErrorResponse("Erro ao enviar mensagem via WhatsApp Service");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem WhatsApp para contato {ContactId}", dto.ContactId);
                return ApiResponse<WhatsAppMessageResponseDto>.ErrorResponse("Erro interno ao enviar mensagem");
            }
        }

        public async Task<ApiResponse<IEnumerable<WhatsAppMessageResponseDto>>> GetMessagesAsync(string phoneNumber, int currentUserId, string currentUserRole, int limit = 500, bool includeMedia = true)
        {
            try
            {
                // Buscar mensagens do WhatsApp Service diretamente pelo número
                _logger.LogInformation("Buscando mensagens para número: {PhoneNumber}, limit: {Limit}, includeMedia: {IncludeMedia}", phoneNumber, limit, includeMedia);
                var response = await _httpClient.GetAsync($"{_whatsappServiceUrl}/messages/{phoneNumber}?limit={limit}&includeMedia={includeMedia.ToString().ToLower()}");
                
                _logger.LogInformation("Resposta do WhatsApp Service: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("JSON recebido: {Json}", json);

                    // O WhatsApp Service pode retornar diretamente uma lista ou um envelope { success, messages }
                    List<WhatsAppServiceMessageDto> serviceMessages = new();
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        JsonElement messagesElement;
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            messagesElement = root;
                        }
                        else if (root.TryGetProperty("messages", out var arr))
                        {
                            messagesElement = arr;
                        }
                        else
                        {
                            messagesElement = default;
                        }

                        if (messagesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in messagesElement.EnumerateArray())
                            {
                                var dto = new WhatsAppServiceMessageDto
                                {
                                    Id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty,
                                    Content = item.TryGetProperty("content", out var contentEl) ? contentEl.GetString() ?? string.Empty : string.Empty,
                                    Sender = item.TryGetProperty("sender", out var senderEl) ? senderEl.GetString() ?? string.Empty : string.Empty,
                                    MediaUrl = item.TryGetProperty("mediaUrl", out var mediaUrlEl) && mediaUrlEl.ValueKind != JsonValueKind.Null ? mediaUrlEl.GetString() : null,
                                    MediaType = item.TryGetProperty("mediaType", out var mediaTypeEl) && mediaTypeEl.ValueKind != JsonValueKind.Null ? mediaTypeEl.GetString() : null,
                                    HasMedia = item.TryGetProperty("hasMedia", out var hasMediaEl) && hasMediaEl.ValueKind == JsonValueKind.True
                                };
                                
                                // Detectar tipo de mídia baseado no conteúdo se MediaType for null
                                if (string.IsNullOrEmpty(dto.MediaType) && !string.IsNullOrEmpty(dto.Content))
                                {
                                    // Verificar se é uma mensagem de fallback (texto com emoji)
                                    if (dto.Content.Contains("🖼️") && (dto.Content.Contains(".jpg") || dto.Content.Contains(".png") || dto.Content.Contains(".jpeg")))
                                    {
                                        dto.MediaType = "image";
                                        // Para mensagens de imagem de fallback, não definir MediaUrl
                                    }
                                    else if (dto.Content.Contains("🎥") && (dto.Content.Contains(".mp4") || dto.Content.Contains(".avi") || dto.Content.Contains(".mov")))
                                    {
                                        dto.MediaType = "video";
                                        // Para mensagens de vídeo de fallback, não definir MediaUrl
                                    }
                                    // Detectar por extensão de arquivo sem emoji
                                    else if (dto.Content.Contains(".wav") || dto.Content.Contains(".mp3") || dto.Content.Contains(".ogg"))
                                    {
                                        dto.MediaType = "audio";
                                    }
                                    else if (dto.Content.Contains(".jpg") || dto.Content.Contains(".jpeg") || dto.Content.Contains(".png") || dto.Content.Contains(".gif"))
                                    {
                                        dto.MediaType = "image";
                                    }
                                    else if (dto.Content.Contains(".mp4") || dto.Content.Contains(".avi") || dto.Content.Contains(".mov"))
                                    {
                                        dto.MediaType = "video";
                                    }
                                }
                                
                                // Se tem MediaUrl mas não tem MediaType, tentar detectar pelo conteúdo da URL
                                if (!string.IsNullOrEmpty(dto.MediaUrl) && string.IsNullOrEmpty(dto.MediaType))
                                {
                                    if (dto.MediaUrl.StartsWith("data:audio/"))
                                    {
                                        dto.MediaType = "audio";
                                    }
                                    else if (dto.MediaUrl.StartsWith("data:image/"))
                                    {
                                        dto.MediaType = "image";
                                    }
                                    else if (dto.MediaUrl.StartsWith("data:video/"))
                                    {
                                        dto.MediaType = "video";
                                    }
                                }
                                
                                _logger.LogInformation("🔍 Debug mensagem do WhatsApp Service - ID: {Id}, Content: {Content}, MediaType: {MediaType}, MediaUrl: {MediaUrl}", 
                                    dto.Id, dto.Content, dto.MediaType, dto.MediaUrl);

                                // Timestamp pode vir como string ISO ou número
                                if (item.TryGetProperty("timestamp", out var tsEl))
                                {
                                    if (tsEl.ValueKind == JsonValueKind.String && DateTime.TryParse(tsEl.GetString(), out var tsDt))
                                        dto.Timestamp = tsDt;
                                    else if (tsEl.ValueKind == JsonValueKind.Number && tsEl.TryGetInt64(out var tsNum))
                                        dto.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tsNum).UtcDateTime;
                                }

                                // Status pode ser número ou string
                                if (item.TryGetProperty("status", out var stEl))
                                {
                                    dto.Status = stEl.ValueKind == JsonValueKind.Number
                                        ? stEl.GetInt32()
                                        : stEl.GetString() ?? "unknown";
                                }

                                serviceMessages.Add(dto);
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Falha ao interpretar JSON de mensagens do WhatsApp Service");
                    }

                    _logger.LogInformation("Mensagens deserializadas: {Count}", serviceMessages?.Count ?? 0);

                    // Converter para o DTO de resposta
                    var messages = serviceMessages?.Select((m, index) => new WhatsAppMessageResponseDto
                    {
                        Id = !string.IsNullOrEmpty(m.Id) && int.TryParse(m.Id, out var id) ? id : index + 1, // Usar ID real ou gerar sequencial
                        ContactId = 0, // Será preenchido pelo frontend
                        Content = m.Content,
                        Sender = m.Sender,
                        Timestamp = m.Timestamp,
                        Status = m.Status?.ToString() ?? "unknown",
                        MediaUrl = m.MediaUrl,
                        MediaType = m.MediaType,
                        HasMedia = m.HasMedia || !string.IsNullOrEmpty(m.MediaUrl) || !string.IsNullOrEmpty(m.MediaType)
                    }).ToList() ?? new List<WhatsAppMessageResponseDto>();

                    return ApiResponse<IEnumerable<WhatsAppMessageResponseDto>>.SuccessResponse(messages);
                }
                else
                {
                    // Logar corpo de erro para diagnóstico
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao buscar mensagens no WhatsApp Service: {Status} {Body}", response.StatusCode, errorBody);
                    return ApiResponse<IEnumerable<WhatsAppMessageResponseDto>>.SuccessResponse(new List<WhatsAppMessageResponseDto>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter mensagens do WhatsApp para número {PhoneNumber}", phoneNumber);
                return ApiResponse<IEnumerable<WhatsAppMessageResponseDto>>.ErrorResponse("Erro interno ao obter mensagens");
            }
        }
    }
}