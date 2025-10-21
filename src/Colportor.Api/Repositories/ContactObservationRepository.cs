using Microsoft.EntityFrameworkCore;
using Colportor.Api.Data;
using Colportor.Api.DTOs;
using Colportor.Api.Repositories.Interfaces;

namespace Colportor.Api.Repositories
{
    public class ContactObservationRepository : IContactObservationRepository
    {
        private readonly AppDbContext _context;

        public ContactObservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ContactObservationDto>> GetByMissionContactIdAsync(int missionContactId)
        {
            return await _context.ContactObservations
                .Where(o => o.MissionContactId == missionContactId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new ContactObservationDto
                {
                    Id = o.Id,
                    MissionContactId = o.MissionContactId,
                    Type = o.Type,
                    Title = o.Title,
                    Content = o.Content,
                    Author = o.Author,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ContactObservationDto> CreateAsync(ContactObservationCreateDto createDto)
        {
            var observation = new Models.ContactObservation
            {
                MissionContactId = createDto.MissionContactId,
                Type = createDto.Type,
                Title = createDto.Title,
                Content = createDto.Content,
                Author = createDto.Author,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactObservations.Add(observation);
            await _context.SaveChangesAsync();

            return new ContactObservationDto
            {
                Id = observation.Id,
                MissionContactId = observation.MissionContactId,
                Type = observation.Type,
                Title = observation.Title,
                Content = observation.Content,
                Author = observation.Author,
                CreatedAt = observation.CreatedAt
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var observation = await _context.ContactObservations.FindAsync(id);
            if (observation == null)
                return false;

            _context.ContactObservations.Remove(observation);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

