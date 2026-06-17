using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Events
{
    public record NotificationEvent(
    string Evento,
    object Data 
);

    public record NuevoMensajeNotification(
        Guid MensajeId,
        Guid ConversacionId,
        Guid EmisorId,
        string Contenido,
        string TipoMensajeCodigo,
        DateTime CreadoAt
    );

    
    public record ChatCreadoNotification(
        Guid ConversacionId,
        bool EsGrupal,
        string? Nombre,
        List<Guid> ParticipantesIds,
        DateTime CreadoAt
    );
    public record ChatLeidoNotification(
        Guid ConversacionId,
        Guid UsuarioId,
        DateTime LeidoAt
    );
}
