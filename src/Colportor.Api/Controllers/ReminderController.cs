using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Colportor.Api.DTOs;
using Colportor.Api.Services;

namespace Colportor.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de lembretes
    /// </summary>
    [ApiController]
    [Route("api/reminders")]
    [Authorize]
    public class ReminderController : BaseController
    {
        private readonly IReminderService _reminderService;

        public ReminderController(IReminderService reminderService, ILogger<ReminderController> logger) : base(logger)
        {
            _reminderService = reminderService;
        }

        /// <summary>
        /// Teste de endpoint
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "ReminderController funcionando!" });
        }

        /// <summary>
        /// Criar um novo lembrete
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ReminderResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateReminder([FromBody] CreateReminderDto createReminderDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var result = await _reminderService.CreateReminderAsync(createReminderDto, userId);
                return result.Success ? CreatedAtAction(nameof(GetReminderById), new { id = result.Data?.Id }, result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao criar lembrete.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter um lembrete por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ReminderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReminderById(int id)
        {
            try
            {
                var reminder = await _reminderService.GetReminderByIdAsync(id);
                if (reminder == null) return NotFound();
                return Ok(reminder);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter lembrete por ID {Id}.", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter lembretes por ID de contato
        /// </summary>
        [HttpGet("contact/{contactId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReminderResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRemindersByContactId(int contactId)
        {
            try
            {
                var reminders = await _reminderService.GetRemindersByContactIdAsync(contactId);
                return Ok(ApiResponse<IEnumerable<ReminderResponseDto>>.SuccessResponse(reminders));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter lembretes para o contato {ContactId}.", contactId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter todos os lembretes do usuário logado
        /// </summary>
        [HttpGet("my-reminders")]
        [ProducesResponseType(typeof(IEnumerable<ReminderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyReminders()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var reminders = await _reminderService.GetMyRemindersAsync(userId);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter lembretes do usuário logado.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter lembretes para hoje do usuário logado
        /// </summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(IEnumerable<ReminderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayReminders()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var reminders = await _reminderService.GetTodayRemindersAsync(userId);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter lembretes de hoje.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter lembretes atrasados do usuário logado
        /// </summary>
        [HttpGet("overdue")]
        [ProducesResponseType(typeof(IEnumerable<ReminderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOverdueReminders()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var reminders = await _reminderService.GetOverdueRemindersAsync(userId);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter lembretes atrasados.");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Atualizar um lembrete existente
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ReminderResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateReminder(int id, [FromBody] UpdateReminderDto updateReminderDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var result = await _reminderService.UpdateReminderAsync(id, updateReminderDto, userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao atualizar lembrete {Id}.", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Marcar um lembrete como completo
        /// </summary>
        [HttpPut("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkReminderAsComplete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var result = await _reminderService.MarkReminderAsCompleteAsync(id, userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao marcar lembrete {Id} como completo.", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Deletar um lembrete
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReminder(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Usuário não autenticado."));

                var result = await _reminderService.DeleteReminderAsync(id, userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao deletar lembrete {Id}.", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }

        /// <summary>
        /// Obter o número de lembretes pendentes para um contato
        /// </summary>
        [HttpGet("contact/{contactId}/count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingRemindersCountByContactId(int contactId)
        {
            try
            {
                var count = await _reminderService.GetPendingRemindersCountByContactIdAsync(contactId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro ao obter contagem de lembretes pendentes para o contato {ContactId}.", contactId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno do servidor."));
            }
        }
    }
}