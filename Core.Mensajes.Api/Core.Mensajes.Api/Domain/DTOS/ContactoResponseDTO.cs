namespace Core.Mensajes.Api.Domain.DTOS
{
    public class ContactoResponseDTO
    {
        public Guid Id { get; set; }
        public string NombreUsuario { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public DateTime? FechaNacimiento { get; set; }
        public string? NumeroTelefono { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreadoAt { get; set; }
    }
}
