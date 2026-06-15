using FluentValidation;
using SideCar.Auth.Api.DTOS;
using SideCar.Auth.Api.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace SideCar.Auth.Api.InfraStructure.Validators
{
    public class RegisterUserValidator:AbstractValidator<RegisterUserDTO>

    {
        private readonly IAuthRepository _authRepository;

        public RegisterUserValidator(IAuthRepository authRepository)
        {
            _authRepository = authRepository;

            RuleFor(x=>x.Email).NotEmpty().EmailAddress()
                .MustAsync(BeUniqueEmail).WithMessage("El email ya se encuentra registrado.");
            RuleFor(x=>x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x=>x.NombreUsuario).NotEmpty().MinimumLength(3);
            RuleFor(x=>x.Nombres).NotEmpty();
            RuleFor(x=>x.Apellidos).NotEmpty();
            RuleFor(x =>x.NumeroTelefono).Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Número de teléfono no válido");
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            var user = await _authRepository.GetUserByEmail(email);
            return user == null;
        }
    }
}
