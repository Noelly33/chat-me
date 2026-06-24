using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Logging;
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
            var msg = context.Message;
            var seq = msg.Seq ?? MessageLogger.NextSeq();

            MessageLogger.Log(seq, "← QUEUE_CONSUME", new()
            {
                { "consumer", nameof(MensajeEnviadoConsumer) },
                { "exchange", "Consumer.Messaging.Worker.Domain.Events:MensajeEnviadoEvent" },
                { "msgId", msg.MensajeId },
                { "conversacion", msg.ConversacionId },
                { "emisor", msg.EmisorId },
                { "tipo", msg.TipoMensajeCodigo },
            });

            try
            {
                await _repository.ProcesarMensajeOnEvent(msg);
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
