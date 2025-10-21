using Colportor.Api.DTOs;
using System.Threading.Tasks;

namespace Colportor.Api.Services.Interfaces
{
    public interface IWhatsAppService
    {
        Task<WhatsAppConnectionStatusDto> GetConnectionStatusAsync();
        Task<ApiResponse<WhatsAppConnectionStatusDto>> ConnectAsync();
        Task<ApiResponse> DisconnectAsync();
        Task<WhatsAppStatsDto> GetStatsAsync();
        Task<ApiResponse<WhatsAppMessageResponseDto>> SendMessageAsync(WhatsAppMessageDto dto, int currentUserId, string currentUserRole);
        Task<ApiResponse<IEnumerable<WhatsAppMessageResponseDto>>> GetMessagesAsync(string phoneNumber, int currentUserId, string currentUserRole);
    }
}