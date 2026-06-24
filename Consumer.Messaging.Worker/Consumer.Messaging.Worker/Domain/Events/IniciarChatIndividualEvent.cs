using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record IniciarChatIndividualEvent(
        Guid MensajeId,
        Guid EmisorId,
        Guid ReceptorId,
        string Contenido,
        string TipoMensajeCodigo,
        [property: JsonPropertyName("_seq")] int? Seq = null
    );
}
