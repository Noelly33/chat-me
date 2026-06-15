namespace SideCar.Auth.Api.DTOS
{
    public class ValidateTokenResponseDTO
    {
        public bool Valid { get; set; }
        public string? Reason { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
