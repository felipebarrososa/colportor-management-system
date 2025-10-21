using Colportor.Api.DTOs;

namespace Colportor.Api.Repositories.Interfaces
{
    public interface IContactObservationRepository
    {
        Task<IEnumerable<ContactObservationDto>> GetByMissionContactIdAsync(int missionContactId);
        Task<ContactObservationDto> CreateAsync(ContactObservationCreateDto createDto);
        Task<bool> DeleteAsync(int id);
    }
}

