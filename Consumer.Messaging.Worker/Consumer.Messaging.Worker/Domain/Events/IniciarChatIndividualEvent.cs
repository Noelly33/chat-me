using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record IniciarChatIndividualEvent(
    Guid MensajeId,         
    Guid EmisorId,           
    Guid ReceptorId,         
    string Contenido,        
    string TipoMensajeCodigo
    );
}
