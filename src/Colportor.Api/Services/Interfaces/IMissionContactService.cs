using Colportor.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Colportor.Api.Services
{
    public interface IMissionContactService
    {
        Task<ApiResponse<MissionContactDto>> CreateAsync(MissionContactCreateDto dto, int currentUserId, string currentUserRole);
        Task<ApiResponse<MissionContactDto>> UpdateAsync(int id, MissionContactUpdateDto dto, int currentUserId, string currentUserRole);
        Task<ApiResponse<MissionContactDto>> UpdateStatusAsync(int id, MissionContactStatusUpdateDto dto, int currentUserId, string currentUserRole);
        Task<ApiResponse<MissionContactDto>> AssignLeaderAsync(int id, int leaderId, int currentUserId, string currentUserRole);
        Task<ApiResponse> DeleteAsync(int id, int currentUserId, string currentUserRole);

        Task<PagedResult<MissionContactDto>> GetPagedAsync(MissionContactFilterDto filter, int currentUserId, string currentUserRole);
        Task<MissionContactDto?> GetByIdAsync(int id, int currentUserId, string currentUserRole);
    }
}



