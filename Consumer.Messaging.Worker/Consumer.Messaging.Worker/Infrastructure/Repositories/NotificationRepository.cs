using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Consumer.Messaging.Worker.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private const string ChannelName = "chat:eventos";

        public NotificationRepository(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task PublicarNuevoMensajeAsync(NuevoMensajeNotification payload)
        {
            var notificacion = new NotificationEvent("nuevo_mensaje", payload);
            await PublishToRedisAsync(notificacion);
        }

        public async Task PublicarChatCreadoAsync(ChatCreadoNotification payload)
        {
            var notificacion = new NotificationEvent("chat_creado", payload);
            await PublishToRedisAsync(notificacion);
        }

        public async Task PublicarChatLeidoAsync(ChatLeidoNotification payload)
        {
            var notificacion = new NotificationEvent("chat_leido", payload);
            await PublishToRedisAsync(notificacion);
        }

        private async Task PublishToRedisAsync(NotificationEvent notification)
        {
            var db = _redis.GetDatabase();

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string json = JsonSerializer.Serialize(notification, options);
            await db.PublishAsync(RedisChannel.Literal(ChannelName), json);
        }
    }
}
