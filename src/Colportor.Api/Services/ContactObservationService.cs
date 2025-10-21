using Colportor.Api.DTOs;
using Colportor.Api.Repositories.Interfaces;
using Colportor.Api.Services.Interfaces;

namespace Colportor.Api.Services
{
    public class ContactObservationService : IContactObservationService
    {
        private readonly IContactObservationRepository _repository;

        public ContactObservationService(IContactObservationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ContactObservationDto>> GetByMissionContactIdAsync(int missionContactId)
        {
            return await _repository.GetByMissionContactIdAsync(missionContactId);
        }

        public async Task<ContactObservationDto> CreateAsync(ContactObservationCreateDto createDto)
        {
            return await _repository.CreateAsync(createDto);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}

