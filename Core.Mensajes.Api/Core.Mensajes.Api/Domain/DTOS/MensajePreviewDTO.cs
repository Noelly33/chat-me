namespace Core.Mensajes.Api.Domain.DTOS
{
    public class MensajePreviewDTO
    {
        public Guid Id { get; set; }
        public string? Contenido { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string TipoMensaje { get; set; } = null!;
        public Guid EmisorId { get; set; }
    }
}
