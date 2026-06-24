using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record MensajeEnviadoEvent(
        Guid MensajeId,
        Guid ConversacionId,
        Guid EmisorId,
        string Contenido,
        string TipoMensajeCodigo,
        [property: JsonPropertyName("_seq")] int? Seq = null
    );
}
