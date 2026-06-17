using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record ChatLeidoEvent(
     Guid ConversacionId,
     Guid UsuarioId,
     DateTime LeidoAt       
    );
}
