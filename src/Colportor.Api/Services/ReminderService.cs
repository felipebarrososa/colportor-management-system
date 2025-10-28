using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories;
using AutoMapper;

namespace Colportor.Api.Services
{
    /// <summary>
    /// Serviço para gerenciamento de lembretes
    /// </summary>
    public class ReminderService : IReminderService
    {
        private readonly IReminderRepository _reminderRepository;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(IReminderRepository reminderRepository, AppDbContext context, IMapper mapper, ILogger<ReminderService> logger)
        {
            _reminderRepository = reminderRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<ReminderResponseDto>> CreateReminderAsync(CreateReminderDto createReminderDto, int userId)
        {
            try
            {
                var reminder = _mapper.Map<Reminder>(createReminderDto);
                reminder.CreatedAt = DateTime.UtcNow;
                reminder.DateTime = DateTime.SpecifyKind(reminder.DateTime, DateTimeKind.Utc);
                reminder.Completed = false;

                await _reminderRepository.AddAsync(reminder);

                var response = _mapper.Map<ReminderResponseDto>(reminder);
                return ApiResponse<ReminderResponseDto>.SuccessResponse(response, "Lembrete criado com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar lembrete.");
                return ApiResponse<ReminderResponseDto>.ErrorResponse("Erro interno do servidor.");
            }
        }

        public async Task<ReminderResponseDto?> GetReminderByIdAsync(int id)
        {
            var reminder = await _reminderRepository.GetByIdWithRelationsAsync(id);
            return reminder != null ? _mapper.Map<ReminderResponseDto>(reminder) : null;
        }

        public async Task<IEnumerable<ReminderResponseDto>> GetRemindersByContactIdAsync(int contactId)
        {
            var reminders = await _reminderRepository.GetByContactIdAsync(contactId);
            var dtos = _mapper.Map<IEnumerable<ReminderResponseDto>>(reminders);
            
            _logger.LogInformation($"🔔 GetRemindersByContactIdAsync - ContactId: {contactId}, Lembretes encontrados: {dtos.Count()}");
            
            // Preencher ContactName para cada lembrete
            var contact = await _context.MissionContacts.FindAsync(contactId);
            if (contact != null)
            {
                _logger.LogInformation($"🔔 Contato encontrado: {contact.FullName}");
                foreach (var dto in dtos)
                {
                    dto.ContactName = contact.FullName;
                }
            }
            else
            {
                _logger.LogWarning($"🔔 Contato NÃO encontrado para ContactId: {contactId}");
            }
            
            return dtos;
        }

        public async Task<IEnumerable<ReminderResponseDto>> GetMyRemindersAsync(int userId)
        {
            var reminders = await _reminderRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ReminderResponseDto>>(reminders);
        }

        public async Task<IEnumerable<ReminderResponseDto>> GetTodayRemindersAsync(int userId)
        {
            var reminders = await _reminderRepository.GetTodayByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ReminderResponseDto>>(reminders);
        }

        public async Task<IEnumerable<ReminderResponseDto>> GetOverdueRemindersAsync(int userId)
        {
            var reminders = await _reminderRepository.GetOverdueByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ReminderResponseDto>>(reminders);
        }

        public async Task<ApiResponse<ReminderResponseDto>> UpdateReminderAsync(int id, UpdateReminderDto updateReminderDto, int userId)
        {
            try
            {
                var reminder = await _reminderRepository.GetByIdAsync(id);
                if (reminder == null)
                    return ApiResponse<ReminderResponseDto>.ErrorResponse("Lembrete não encontrado.");

                _mapper.Map(updateReminderDto, reminder);
                if (updateReminderDto.Completed == true && reminder.CompletedAt == null)
                {
                    reminder.CompletedAt = DateTime.UtcNow;
                }
                else if (updateReminderDto.Completed == false)
                {
                    reminder.CompletedAt = null;
                }
                
                // Garantir que DateTime seja UTC
                if (updateReminderDto.DateTime.HasValue)
                {
                    reminder.DateTime = DateTime.SpecifyKind(updateReminderDto.DateTime.Value, DateTimeKind.Utc);
                }

                await _reminderRepository.UpdateAsync(reminder);

                var response = _mapper.Map<ReminderResponseDto>(reminder);
                return ApiResponse<ReminderResponseDto>.SuccessResponse(response, "Lembrete atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar lembrete {Id}.", id);
                return ApiResponse<ReminderResponseDto>.ErrorResponse("Erro interno do servidor.");
            }
        }

        public async Task<ApiResponse<object>> DeleteReminderAsync(int id, int userId)
        {
            try
            {
                var reminder = await _reminderRepository.GetByIdAsync(id);
                if (reminder == null)
                    return ApiResponse<object>.ErrorResponse("Lembrete não encontrado.");

                await _reminderRepository.RemoveAsync(reminder);

                return ApiResponse<object>.SuccessResponse(new object(), "Lembrete deletado com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar lembrete {Id}.", id);
                return ApiResponse<object>.ErrorResponse("Erro interno do servidor.");
            }
        }

        public async Task<ApiResponse<object>> MarkReminderAsCompleteAsync(int id, int userId)
        {
            try
            {
                var reminder = await _reminderRepository.GetByIdAsync(id);
                if (reminder == null)
                    return ApiResponse<object>.ErrorResponse("Lembrete não encontrado.");

                if (!reminder.Completed)
                {
                    reminder.Completed = true;
                    reminder.CompletedAt = DateTime.UtcNow;
                    await _reminderRepository.UpdateAsync(reminder);
                }

                return ApiResponse<object>.SuccessResponse(new object(), "Lembrete marcado como completo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao marcar lembrete {Id} como completo.", id);
                return ApiResponse<object>.ErrorResponse("Erro interno do servidor.");
            }
        }

        public async Task<ReminderSummaryDto> GetReminderSummaryByContactIdAsync(int contactId)
        {
            var now = DateTime.UtcNow;
            var reminders = await _reminderRepository.GetByContactIdAsync(contactId);
            var pendingReminders = reminders.Where(r => !r.Completed).ToList();

            return new ReminderSummaryDto
            {
                Total = pendingReminders.Count,
                Overdue = pendingReminders.Count(r => r.DateTime < now),
                Today = pendingReminders.Count(r => r.DateTime.Date == now.Date),
                Future = pendingReminders.Count(r => r.DateTime > now)
            };
        }

        public async Task<int> GetPendingRemindersCountByContactIdAsync(int contactId)
        {
            return await _reminderRepository.GetPendingCountByContactIdAsync(contactId);
        }
    }
}