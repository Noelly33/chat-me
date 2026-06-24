using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Logging;
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
            var msg = context.Message;
            var seq = msg.Seq ?? MessageLogger.NextSeq();

            MessageLogger.Log(seq, "← QUEUE_CONSUME", new()
            {
                { "consumer", nameof(ChatLeidoConsumer) },
                { "exchange", "Consumer.Messaging.Worker.Domain.Events:ChatLeidoEvent" },
                { "conversacion", msg.ConversacionId },
                { "usuario", msg.UsuarioId },
            });

            try
            {
                await _repository.ProcesarLecturaMensajeOnEvent(msg);
                MessageLogger.Log(seq, "✓ QUEUE_CONSUME_OK", new()
                {
                    { "conversacion", msg.ConversacionId },
                    { "usuario", msg.UsuarioId },
                });
            }
            catch (Exception ex)
            {
                MessageLogger.Log(seq, "✗ QUEUE_CONSUME_FAIL", new()
                {
                    { "conversacion", msg.ConversacionId },
                    { "error", ex.GetType().Name },
                    { "msg", ex.Message },
                });
                throw;
            }
        }
    }
}
