using Consumer.Messaging.Worker.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Repositories
{
    public interface INotificationRepository
    {
        Task PublicarNuevoMensajeAsync(NuevoMensajeNotification payload);
        Task PublicarChatCreadoAsync(ChatCreadoNotification payload);
        Task PublicarChatLeidoAsync(ChatLeidoNotification payload);
    }
}
