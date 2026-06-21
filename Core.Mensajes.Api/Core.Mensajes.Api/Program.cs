using Core.Mensajes.Api.Application;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Domain.Services;
using Core.Mensajes.Api.Infrastructure.Context;
using Core.Mensajes.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionsStrings = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(connectionsStrings));

builder.Services.AddScoped<IContactoRepository, ContactRepository>();
builder.Services.AddScoped<IContactoService, ContactoService>();
builder.Services.AddScoped<IMessageRepository, MensajesRepository>();
builder.Services.AddScoped<IMessagesService, MessageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
