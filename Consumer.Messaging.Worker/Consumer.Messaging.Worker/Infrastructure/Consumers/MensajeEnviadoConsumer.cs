using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using MassTransit;


namespace Consumer.Messaging.Worker.Infrastructure.Consumers
{
    public class MensajeEnviadoConsumer : IConsumer<MensajeEnviadoEvent>
    {
        private readonly IMessageRepository _repository;

        public MensajeEnviadoConsumer(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<MensajeEnviadoEvent> context)
        {

            await _repository.ProcesarMensajeOnEvent(context.Message);
        }
    }
}
