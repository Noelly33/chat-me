namespace SideCar.Auth.Api.DTOS
{
    public class RegisterResultDTO
    {
        public string Email { get; set; }
         public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiration { get; set; }

    }
}
