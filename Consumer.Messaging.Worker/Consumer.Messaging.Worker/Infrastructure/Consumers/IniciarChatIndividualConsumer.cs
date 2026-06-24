using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Logging;
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
            var msg = context.Message;
            var seq = msg.Seq ?? MessageLogger.NextSeq();

            MessageLogger.Log(seq, "← QUEUE_CONSUME", new()
            {
                { "consumer", nameof(IniciarChatIndividualConsumer) },
                { "exchange", "Consumer.Messaging.Worker.Domain.Events:IniciarChatIndividualEvent" },
                { "msgId", msg.MensajeId },
                { "emisor", msg.EmisorId },
                { "receptor", msg.ReceptorId },
                { "tipo", msg.TipoMensajeCodigo },
            });

            try
            {
                await _repository.ProcesarNuevoChatUsuarioOnEvent(msg);
                MessageLogger.Log(seq, "✓ QUEUE_CONSUME_OK", new() { { "msgId", msg.MensajeId } });
            }
            catch (Exception ex)
            {
                MessageLogger.Log(seq, "✗ QUEUE_CONSUME_FAIL", new()
                {
                    { "msgId", msg.MensajeId },
                    { "error", ex.GetType().Name },
                    { "msg", ex.Message },
                });
                throw;
            }
        }
    }
}
