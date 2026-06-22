using Consumer.Messaging.Worker;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Consumers;
using Consumer.Messaging.Worker.Infrastructure.Context;
using Consumer.Messaging.Worker.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

var rabbitHost        = builder.Configuration["RabbitMq:Host"]        ?? "localhost";
var rabbitVirtualHost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";
var rabbitUsername    = builder.Configuration["RabbitMq:Username"]    ?? "guest";
var rabbitPassword    = builder.Configuration["RabbitMq:Password"]    ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MensajeEnviadoConsumer>();
    x.AddConsumer<IniciarChatIndividualConsumer>();
    x.AddConsumer<ChatLeidoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitVirtualHost, h =>
        {
            h.Username(rabbitUsername);
            h.Password(rabbitPassword);
        });
        cfg.ConfigureEndpoints(context);
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
