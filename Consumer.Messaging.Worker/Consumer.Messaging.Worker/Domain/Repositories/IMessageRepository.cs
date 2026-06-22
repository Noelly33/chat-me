using Consumer.Messaging.Worker.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Domain.Repositories
{
    public interface IMessageRepository
    {
        Task ProcesarMensajeOnEvent(MensajeEnviadoEvent mensajeEnviadoEvent);

        Task ProcesarNuevoChatUsuarioOnEvent(IniciarChatIndividualEvent iniciarChatIndividualEvent);
        Task ProcesarLecturaMensajeOnEvent(ChatLeidoEvent lecturaMensajeEvent);
    }
}
