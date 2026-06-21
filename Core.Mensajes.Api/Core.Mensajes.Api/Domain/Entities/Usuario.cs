using Consumer.Messaging.Worker.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Mensajes.Api.Domain.Entities
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("nombre_usuario")]
        public string NombreUsuario { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = null!;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [Column("nombres")]
        public string Nombres { get; set; } = null!;

        [Required]
        [Column("apellidos")]
        public string Apellidos { get; set; } = null!;

        [Column("fecha_nacimiento", TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }

        [Column("numero_telefono")]
        public string? NumeroTelefono { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("creado_at")]
        public DateTime CreadoAt { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación (Relación 1 a Muchos con los tokens)
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public virtual ICollection<Mensaje> MensajesEnviados { get; set; } = new List<Mensaje>();
    }
    }
