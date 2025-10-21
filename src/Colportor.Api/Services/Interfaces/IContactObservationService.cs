using Colportor.Api.DTOs;

namespace Colportor.Api.Services.Interfaces
{
    public interface IContactObservationService
    {
        Task<IEnumerable<ContactObservationDto>> GetByMissionContactIdAsync(int missionContactId);
        Task<ContactObservationDto> CreateAsync(ContactObservationCreateDto createDto);
        Task<bool> DeleteAsync(int id);
    }
}

