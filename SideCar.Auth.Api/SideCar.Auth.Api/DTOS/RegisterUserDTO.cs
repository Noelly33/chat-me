namespace SideCar.Auth.Api.DTOS
{
    public class RegisterUserDTO
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        public string NumeroTelefono { get; set; }

        public string Password { get; set; }
        public string Email { get; set; }
    
        public DateTime? FechaNacimiento { get; set; } = DateTime.MinValue;

        public string NombreUsuario { get; set; }
    }
}
