using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Entities
{
    public class Conversacion
    {
        public Guid Id { get; set; }
        public bool EsGrupal { get; set; }
        public string? Nombre { get; set; }
        public DateTime CreadoAt { get; set; }

        // Propiedades de navegación (Relaciones)
        public ICollection<ParticipanteConversacion> Participantes { get; set; } = new List<ParticipanteConversacion>();
        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
    }
}
