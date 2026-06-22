namespace SideCar.Auth.Api.DTOS
{
    public class UpdateUserDTO
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? NumeroTelefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
