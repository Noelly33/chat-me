namespace SideCar.Auth.Api.DTOS
{
    public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiration);
}
