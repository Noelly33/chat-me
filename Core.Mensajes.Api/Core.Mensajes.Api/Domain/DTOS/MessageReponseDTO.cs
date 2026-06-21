using Consumer.Messaging.Worker.Domain.Entities;
using Core.Mensajes.Api.Domain.Entities;

namespace Core.Mensajes.Api.Domain.DTOS
{
    public class MessageReponseDTO
    {
        public Guid Id { get; set; }
        public String TipoMensaje { get; set; } = null!;
        public string Contenido { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
        public Guid EmisorId { get; set; }
        public string EmisorNombre { get; set; } = null!;
    }
}
