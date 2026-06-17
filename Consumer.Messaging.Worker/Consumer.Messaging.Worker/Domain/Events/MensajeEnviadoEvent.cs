using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record MensajeEnviadoEvent(
    Guid MensajeId,          
    Guid ConversacionId,     
    Guid EmisorId,           
    string Contenido,        
    string TipoMensajeCodigo
    );
}
