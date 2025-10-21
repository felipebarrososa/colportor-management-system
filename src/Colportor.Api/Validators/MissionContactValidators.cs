using FluentValidation;
using Colportor.Api.DTOs;
using System;

namespace Colportor.Api.Validators
{
    public class MissionContactCreateDtoValidator : AbstractValidator<MissionContactCreateDto>
    {
        public MissionContactCreateDtoValidator()
        {
            RuleFor(x => x.FullName).NotEmpty();
            RuleFor(x => x.RegionId).GreaterThan(0);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.BirthDate).LessThan(DateTime.UtcNow).When(x => x.BirthDate.HasValue);
            RuleFor(x => x.AvailableDate).GreaterThan(DateTime.UtcNow).When(x => x.AvailableDate.HasValue);
            RuleFor(x => x.FluencyLevel).Must(x => x == null || x == "Básico" || x == "Avançado");
        }
    }

    public class MissionContactUpdateDtoValidator : AbstractValidator<MissionContactUpdateDto>
    {
        public MissionContactUpdateDtoValidator()
        {
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.BirthDate).LessThan(DateTime.UtcNow).When(x => x.BirthDate.HasValue);
            RuleFor(x => x.AvailableDate).GreaterThan(DateTime.UtcNow).When(x => x.AvailableDate.HasValue);
            RuleFor(x => x.FluencyLevel).Must(x => x == null || x == "Básico" || x == "Avançado");
        }
    }

    public class MissionContactStatusUpdateDtoValidator : AbstractValidator<MissionContactStatusUpdateDto>
    {
        private static readonly string[] AllowedStatuses = new[] { "Novo", "Contato iniciado", "Interessado", "Agendado", "Inscrito", "Não interessado", "Arquivado" };

        public MissionContactStatusUpdateDtoValidator()
        {
            RuleFor(x => x.Status).NotEmpty().Must(s => AllowedStatuses.Contains(s));
        }
    }
}



