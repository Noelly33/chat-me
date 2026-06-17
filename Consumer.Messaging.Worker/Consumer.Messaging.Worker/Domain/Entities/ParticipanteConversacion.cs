using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Entities
{
    public class ParticipanteConversacion
    {
        public Guid ConversacionId { get; set; }
        public Guid UsuarioId { get; set; }
        public DateTime CreadoAt { get; set; } // Equivalente a unido_at
        public DateTime? UltimoLeidoAt { get; set; }

        // Propiedad de navegación
        public Conversacion Conversacion { get; set; } = null!;
    }
}
