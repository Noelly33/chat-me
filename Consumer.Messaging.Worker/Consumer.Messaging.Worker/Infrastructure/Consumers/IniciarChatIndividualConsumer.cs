using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Infrastructure.Consumers
{
    public class IniciarChatIndividualConsumer : IConsumer<IniciarChatIndividualEvent>
    {
        private readonly IMessageRepository _repository;

        public IniciarChatIndividualConsumer(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<IniciarChatIndividualEvent> context)
        {
            await _repository.ProcesarNuevoChatUsuarioOnEvent(context.Message);
        }
    }
}
