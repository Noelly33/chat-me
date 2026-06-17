using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Infrastructure.Consumers
{
    public class ChatLeidoConsumer : IConsumer<ChatLeidoEvent>
    {
        private readonly IMessageRepository _repository;

        public ChatLeidoConsumer(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<ChatLeidoEvent> context)
        {
            await _repository.ProcesarLecturaMensajeOnEvent(context.Message);
        }
    }
}
