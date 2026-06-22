namespace SideCar.Auth.Api.DTOS
{
    public class ResfreshTokenRequestDTO
    {
        public string? Token { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
