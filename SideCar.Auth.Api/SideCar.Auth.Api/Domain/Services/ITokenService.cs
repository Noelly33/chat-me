using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.Domain.Services
{
    public interface ITokenService
    {
        Task<TokenResponseDto> GenerarTokens(RegisterUserDTO usuario, Guid usuarioId);

        Task<TokenResponseDto> ResfreshToken(ResfreshTokenRequestDTO request);

        ValidateTokenResponseDTO ValidateToken(string token);

    }
}
