using Colportor.Api.DTOs;

namespace Colportor.Api.Services
{
    /// <summary>
    /// Interface para serviços de lembretes
    /// </summary>
    public interface IReminderService
    {
        /// <summary>
        /// Cria um novo lembrete
        /// </summary>
        Task<ApiResponse<ReminderResponseDto>> CreateReminderAsync(CreateReminderDto createReminderDto, int userId);

        /// <summary>
        /// Obtém lembrete por ID
        /// </summary>
        Task<ReminderResponseDto?> GetReminderByIdAsync(int id);

        /// <summary>
        /// Obtém lembretes por contato
        /// </summary>
        Task<IEnumerable<ReminderResponseDto>> GetRemindersByContactIdAsync(int contactId);

        /// <summary>
        /// Obtém lembretes do usuário
        /// </summary>
        Task<IEnumerable<ReminderResponseDto>> GetMyRemindersAsync(int userId);

        /// <summary>
        /// Obtém lembretes de hoje
        /// </summary>
        Task<IEnumerable<ReminderResponseDto>> GetTodayRemindersAsync(int userId);

        /// <summary>
        /// Obtém lembretes atrasados
        /// </summary>
        Task<IEnumerable<ReminderResponseDto>> GetOverdueRemindersAsync(int userId);

        /// <summary>
        /// Atualiza lembrete
        /// </summary>
        Task<ApiResponse<ReminderResponseDto>> UpdateReminderAsync(int id, UpdateReminderDto updateReminderDto, int userId);

        /// <summary>
        /// Deleta lembrete
        /// </summary>
        Task<ApiResponse<object>> DeleteReminderAsync(int id, int userId);

        /// <summary>
        /// Marca lembrete como completo
        /// </summary>
        Task<ApiResponse<object>> MarkReminderAsCompleteAsync(int id, int userId);

        /// <summary>
        /// Obtém resumo de lembretes por contato
        /// </summary>
        Task<ReminderSummaryDto> GetReminderSummaryByContactIdAsync(int contactId);

        /// <summary>
        /// Obtém contagem de lembretes pendentes por contato
        /// </summary>
        Task<int> GetPendingRemindersCountByContactIdAsync(int contactId);
    }
}

