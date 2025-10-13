using FluentValidation;
using Colportor.Api.DTOs;

namespace Colportor.Api.Validators;

/// <summary>
/// Validador para CreateColportorDto
/// </summary>
public class CreateColportorDtoValidator : AbstractValidator<CreateColportorDto>
{
    public CreateColportorDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras e espaços");

        RuleFor(x => x.CPF)
            .NotEmpty().WithMessage("CPF é obrigatório")
            .Matches(@"^\d{11}$").WithMessage("CPF deve conter exatamente 11 dígitos")
            .Must(IsValidCPF).WithMessage("CPF inválido");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email deve ter um formato válido")
            .MaximumLength(255).WithMessage("Email deve ter no máximo 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter pelo menos 6 caracteres")
            .MaximumLength(100).WithMessage("Senha deve ter no máximo 100 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Senha deve conter pelo menos uma letra minúscula, uma maiúscula e um número");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gênero é obrigatório")
            .Must(g => g == "Masculino" || g == "Feminino").WithMessage("Gênero deve ser 'Masculino' ou 'Feminino'");

        RuleFor(x => x.RegionId)
            .GreaterThan(0).WithMessage("Região é obrigatória");

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Now.AddYears(-14)).WithMessage("Idade mínima é 14 anos")
            .GreaterThan(DateTime.Now.AddYears(-100)).WithMessage("Data de nascimento inválida");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres");

        RuleFor(x => x.PhotoUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.PhotoUrl))
            .WithMessage("URL da foto deve ser válida");
    }

    private static bool IsValidCPF(string cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length != 11)
            return false;

        // Verificar se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Calcular primeiro dígito verificador
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        int remainder = sum % 11;
        int firstDigit = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cpf[9].ToString()) != firstDigit)
            return false;

        // Calcular segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        int secondDigit = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cpf[10].ToString()) == secondDigit;
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
