using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Services;
using Colportor.Api.DTOs;

namespace Colportor.Api.Mapping;

/// <summary>
/// Profile do AutoMapper para mapeamento de entidades
/// </summary>
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<User, AuthResultDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        // Colportor mappings
        CreateMap<Models.Colportor, ColportorDto>()
            .ForMember(dest => dest.RegionName, opt => opt.MapFrom(src => src.Region!.Name))
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader!.FullName ?? src.Leader.Email))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CalculateStatus(src.LastVisitDate)))
            .ForMember(dest => dest.Visits, opt => opt.MapFrom(src => src.Visits))
            .ForMember(dest => dest.PacEnrollments, opt => opt.MapFrom(src => src.PacEnrollments));

        CreateMap<CreateColportorDto, Models.Colportor>();
        CreateMap<UpdateColportorDto, Models.Colportor>();

        // Visit mappings
        CreateMap<Visit, VisitDto>();

        // PacEnrollment mappings
        CreateMap<PacEnrollment, PacEnrollmentDto>()
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader.FullName ?? src.Leader.Email))
            .ForMember(dest => dest.ColportorName, opt => opt.MapFrom(src => src.Colportor.FullName))
            .ForMember(dest => dest.ColportorCPF, opt => opt.MapFrom(src => src.Colportor.CPF))
            .ForMember(dest => dest.ColportorGender, opt => opt.MapFrom(src => src.Colportor.Gender))
            .ForMember(dest => dest.ColportorRegionId, opt => opt.MapFrom(src => src.Colportor.RegionId))
            .ForMember(dest => dest.ColportorRegionName, opt => opt.MapFrom(src => src.Colportor.Region.Name));

               // Region mappings
               CreateMap<Region, RegionDto>()
                   .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country!.Name));

               CreateMap<RegionCreateDto, Region>();

               // Country mappings
               CreateMap<Country, CountryDto>()
                   .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Iso2 ?? ""))
                   .ForMember(dest => dest.RegionsCount, opt => opt.MapFrom(src => src.Regions.Count));
               CreateMap<CountryCreateDto, Country>()
                   .ForMember(dest => dest.Iso2, opt => opt.MapFrom(src => src.Code));

        // Calendar mappings
        CreateMap<Models.Colportor, ColportorSummaryDto>()
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader!.FullName ?? src.Leader.Email));

        // MissionContact mappings
        CreateMap<Models.MissionContact, MissionContactDto>()
            .ForMember(dest => dest.RegionName, opt => opt.MapFrom(src => src.Region != null ? src.Region.Name : null))
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader != null ? (src.Leader.FullName ?? src.Leader.Email) : null))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));

        // WhatsApp mappings
        CreateMap<WhatsAppMessage, WhatsAppMessageResponseDto>();
        CreateMap<WhatsAppTemplate, WhatsAppTemplateDto>();
        CreateMap<WhatsAppTemplateCreateDto, WhatsAppTemplate>();
        CreateMap<WhatsAppConnection, WhatsAppConnectionStatusDto>();
    }

    /// <summary>
    /// Calcula o status do colportor baseado na última visita
    /// </summary>
    private static string CalculateStatus(DateTime? lastVisitDate)
    {
        if (!lastVisitDate.HasValue)
        {
            return "PENDENTE";
        }

        var dueDate = lastVisitDate.Value.AddYears(1);
        var today = DateTime.UtcNow.Date;
        var daysUntilDue = (dueDate.Date - today).TotalDays;

        if (daysUntilDue < 0)
        {
            return "VENCIDO";
        }
        else if (daysUntilDue <= 30)
        {
            return "AVISO";
        }
        else
        {
            return "EM DIA";
        }
    }

    private static int? CalculateAge(DateTime? birthDate)
    {
        if (!birthDate.HasValue) return null;
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Value.Date.Year;
        if (birthDate.Value.Date > today.AddYears(-age)) age--;
        return age;
    }
}
