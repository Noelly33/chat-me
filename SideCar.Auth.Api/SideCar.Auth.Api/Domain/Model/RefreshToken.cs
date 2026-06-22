using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideCar.Auth.Api.Domain.Model
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("usuario_id")]
        public Guid UsuarioId { get; set; }

        [Required]
        [Column("token")]
        public string Token { get; set; } = null!;

        [Required]
        [Column("fecha_expiracion")]
        public DateTime FechaExpiracion { get; set; }

        [Required]
        [Column("esta_revocado")]
        public bool EstaRevocado { get; set; } = false;

        [Column("creado_at")]
        public DateTime CreadoAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario Usuario { get; set; } = null!;

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= FechaExpiracion;

        [NotMapped]
        public bool IsActive => !EstaRevocado && !IsExpired;
    }
   }
