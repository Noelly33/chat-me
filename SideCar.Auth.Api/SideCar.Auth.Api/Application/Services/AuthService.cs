using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.Domain.Repositories;
using SideCar.Auth.Api.Domain.Services;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private IMapper _mapper;
        private IAuthRepository _authRepository;
        private ITokenService _tokenService;
        private readonly PasswordHasher<Usuario> _passwordHasher = new();


        public AuthService(IMapper mapper,IAuthRepository authRepository,ITokenService tokenService)
        {
            _mapper = mapper;
            this._authRepository = authRepository;
            _tokenService = tokenService;
        }
        public async Task<RegisterResultDTO> register(RegisterUserDTO request)
        {
           var user = _mapper.Map<Usuario>(request);
            await _authRepository.registerUser(user);
            await _authRepository.Save();
            TokenResponseDto tokenResponse = await _tokenService.GenerarTokens(request,user.Id);

            var userResult = new RegisterResultDTO {
                Email=user.Email,
                Token=tokenResponse.AccessToken,
                RefreshToken=tokenResponse.RefreshToken,
                RefreshTokenExpiration=tokenResponse.RefreshTokenExpiration
            };

            return userResult;
        }

        public async Task<UpdateUserResultDTO> UpdateUser(Guid userId, UpdateUserDTO request)
        {
            var usuario = await _authRepository.GetUserById(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado");

            if (request.Nombres != null) usuario.Nombres = request.Nombres;
            if (request.Apellidos != null) usuario.Apellidos = request.Apellidos;
            if (request.NumeroTelefono != null) usuario.NumeroTelefono = request.NumeroTelefono;
            if (request.FechaNacimiento.HasValue)
            {
                usuario.FechaNacimiento = request.FechaNacimiento.Value == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc);
            }
            if (request.AvatarUrl != null) usuario.AvatarUrl = request.AvatarUrl;

            await _authRepository.Save();

            return new UpdateUserResultDTO
            {
                Id = usuario.Id,
                Email = usuario.Email,
                NombreUsuario = usuario.NombreUsuario,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                NumeroTelefono = usuario.NumeroTelefono,
                FechaNacimiento = usuario.FechaNacimiento,
                AvatarUrl = usuario.AvatarUrl
            };
        }

        public async Task<LoginResultDTO> Login(LoginUserDTO request)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            var usuario = await _authRepository.GetUserByEmail(identifier)
                          ?? await _authRepository.GetUserByUsername(identifier)
                          ?? throw new UnauthorizedAccessException("Credenciales inválidas");

            var verification = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Credenciales inválidas");
            }

            var registerDto = new RegisterUserDTO
            {
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email,
                NombreUsuario = usuario.NombreUsuario,
                NumeroTelefono = usuario.NumeroTelefono ?? string.Empty,
                FechaNacimiento = usuario.FechaNacimiento ?? DateTime.MinValue,
                Password = string.Empty
            };
            var tokenResponse = await _tokenService.GenerarTokens(registerDto, usuario.Id);

            return new LoginResultDTO
            {
                Email = usuario.Email,
                Token = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                RefreshTokenExpiration = tokenResponse.RefreshTokenExpiration
            };
        }
    }
}
