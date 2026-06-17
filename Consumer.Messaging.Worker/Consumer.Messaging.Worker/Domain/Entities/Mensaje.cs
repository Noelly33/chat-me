using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Entities
{
    public class Mensaje
    {
        public Guid Id { get; set; }
        public Guid ConversacionId { get; set; }
        public Guid EmisorId { get; set; }
        public int TipoMensajeId { get; set; }
        public string? Contenido { get; set; }
        public DateTime CreadoAt { get; set; }

        public Conversacion Conversacion { get; set; } = null!;
        public TipoMensaje TipoMensaje { get; set; } = null!;
    }
}
