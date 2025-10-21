using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Colportor.Api.DTOs;
using Colportor.Api.Services.Interfaces;

namespace Colportor.Api.Controllers
{
    [ApiController]
    [Route("api/mission-contacts/{missionContactId}/observations")]
    [Authorize(Roles = "Admin,Leader")]
    public class ContactObservationsController : ControllerBase
    {
        private readonly IContactObservationService _observationService;

        public ContactObservationsController(IContactObservationService observationService)
        {
            _observationService = observationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetObservations(int missionContactId)
        {
            try
            {
                var observations = await _observationService.GetByMissionContactIdAsync(missionContactId);
                return Ok(observations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateObservation(int missionContactId, [FromBody] ContactObservationCreateDto createDto)
        {
            try
            {
                if (createDto.MissionContactId != missionContactId)
                {
                    return BadRequest(new { message = "ID do contato missionário não confere" });
                }

                var observation = await _observationService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetObservations), new { missionContactId }, observation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObservation(int missionContactId, int id)
        {
            try
            {
                var deleted = await _observationService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Observação não encontrada" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }
    }
}

