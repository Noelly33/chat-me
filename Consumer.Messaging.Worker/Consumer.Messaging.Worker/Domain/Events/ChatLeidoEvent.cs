using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record ChatLeidoEvent(
        Guid ConversacionId,
        Guid UsuarioId,
        DateTime LeidoAt,
        [property: JsonPropertyName("_seq")] int? Seq = null
    );
}
