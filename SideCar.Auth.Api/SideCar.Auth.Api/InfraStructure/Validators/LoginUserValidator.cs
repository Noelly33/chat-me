using FluentValidation;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.InfraStructure.Validators
{
    public class LoginUserValidator : AbstractValidator<LoginUserDTO>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.Identifier).NotEmpty().WithMessage("Identifier es requerido");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password es requerido");
        }
    }
}
