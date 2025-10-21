using System;
using System.Collections.Generic;

namespace Colportor.Api.DTOs
{
    public class MissionContactCreateDto
    {
        public string FullName { get; set; } = default!;
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Profession { get; set; }
        public bool SpeaksOtherLanguages { get; set; }
        public string? OtherLanguages { get; set; }
        public string? FluencyLevel { get; set; }
        public string? Church { get; set; }
        public string? ConversionTime { get; set; }
        public string? MissionsDedicationPlan { get; set; }
        public bool HasPassport { get; set; }
        public DateTime? AvailableDate { get; set; }

        public int RegionId { get; set; }
        public int? LeaderId { get; set; }
    }

    public class MissionContactUpdateDto
    {
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Profession { get; set; }
        public bool? SpeaksOtherLanguages { get; set; }
        public string? OtherLanguages { get; set; }
        public string? FluencyLevel { get; set; }
        public string? Church { get; set; }
        public string? ConversionTime { get; set; }
        public string? MissionsDedicationPlan { get; set; }
        public bool? HasPassport { get; set; }
        public DateTime? AvailableDate { get; set; }
        public int? RegionId { get; set; }
        public int? LeaderId { get; set; }
        public string? Notes { get; set; }
    }

    public class MissionContactStatusUpdateDto
    {
        public string Status { get; set; } = default!;
        public DateTime? LastContactedAt { get; set; }
        public DateTime? NextFollowUpAt { get; set; }
        public string? Notes { get; set; }
    }

    public class MissionContactAssignDto
    {
        public int LeaderId { get; set; }
    }

    public class MissionContactDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Age { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Profession { get; set; }
        public bool SpeaksOtherLanguages { get; set; }
        public string? OtherLanguages { get; set; }
        public string? FluencyLevel { get; set; }
        public string? Church { get; set; }
        public string? ConversionTime { get; set; }
        public string? MissionsDedicationPlan { get; set; }
        public bool HasPassport { get; set; }
        public DateTime? AvailableDate { get; set; }

        public int RegionId { get; set; }
        public string? RegionName { get; set; }
        public int? LeaderId { get; set; }
        public string? LeaderName { get; set; }

        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime? LastContactedAt { get; set; }
        public DateTime? NextFollowUpAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MissionContactFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? RegionId { get; set; }
        public int? LeaderId { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }
    }
}



