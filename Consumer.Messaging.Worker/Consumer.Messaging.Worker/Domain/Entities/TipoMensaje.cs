using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Entities
{
    public class TipoMensaje
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Descripcion { get; set; }

        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
    }
}
