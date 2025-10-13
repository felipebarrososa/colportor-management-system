using AutoMapper;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Services;

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
        CreateMap<Colportor, ColportorDto>()
            .ForMember(dest => dest.RegionName, opt => opt.MapFrom(src => src.Region!.Name))
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader!.FullName ?? src.Leader.Email))
            .ForMember(dest => dest.Visits, opt => opt.MapFrom(src => src.Visits))
            .ForMember(dest => dest.PacEnrollments, opt => opt.MapFrom(src => src.PacEnrollments));

        CreateMap<CreateColportorDto, Colportor>();
        CreateMap<UpdateColportorDto, Colportor>();

        // Visit mappings
        CreateMap<Visit, VisitDto>();

        // PacEnrollment mappings
        CreateMap<PacEnrollment, PacEnrollmentDto>()
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader.FullName ?? src.Leader.Email));

        // Region mappings
        CreateMap<Region, RegionDto>()
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country!.Name));

        CreateMap<RegionCreateDto, Region>();

        // Calendar mappings
        CreateMap<Colportor, ColportorSummaryDto>()
            .ForMember(dest => dest.LeaderName, opt => opt.MapFrom(src => src.Leader!.FullName ?? src.Leader.Email));
    }
}
