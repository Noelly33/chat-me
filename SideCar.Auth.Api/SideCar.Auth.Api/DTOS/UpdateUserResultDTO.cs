namespace SideCar.Auth.Api.DTOS
{
    public class UpdateUserResultDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? NumeroTelefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
