using FluentValidation;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.InfraStructure.Validators
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserDTO>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Nombres)
                .NotEmpty().When(x => x.Nombres != null)
                .WithMessage("Nombres no puede estar vacío");

            RuleFor(x => x.Apellidos)
                .NotEmpty().When(x => x.Apellidos != null)
                .WithMessage("Apellidos no puede estar vacío");

            RuleFor(x => x.NumeroTelefono)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .When(x => !string.IsNullOrEmpty(x.NumeroTelefono))
                .WithMessage("Número de teléfono no válido");

            RuleFor(x => x.AvatarUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
                .WithMessage("AvatarUrl debe ser una URL válida");
        }
    }
}
