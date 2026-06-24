using System;

namespace Core.Mensajes.Api.Domain.DTOS
{
    public class ConversacionListadoDTO
    {
        public Guid Id { get; set; }
        public ContactoResponseDTO OtroUsuario { get; set; } = null!;
        public MensajePreviewDTO? UltimoMensaje { get; set; }
        public DateTime CreadoAt { get; set; }
    }
}
