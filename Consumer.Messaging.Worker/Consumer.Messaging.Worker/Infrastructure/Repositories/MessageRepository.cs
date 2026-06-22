using Consumer.Messaging.Worker.Domain.Entities;
using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDbContext _context;
        private readonly INotificationRepository _notificationRepository;

        public MessageRepository(ChatDbContext context, INotificationRepository notificationRepository)
        {
            _context = context;
            _notificationRepository = notificationRepository;
        }

        public async Task ProcesarMensajeOnEvent(MensajeEnviadoEvent ev)
        {
            var isExists = await _context.Mensajes.AnyAsync(m => m.Id == ev.MensajeId);
            if (isExists) return;

            var tipoMensaje = await _context.TiposMensaje
                .FirstOrDefaultAsync(t => t.Codigo == ev.TipoMensajeCodigo);

            int tipoId = tipoMensaje?.Id ?? 1; 

            var nuevoMensaje = new Mensaje
            {
                Id = ev.MensajeId,
                ConversacionId = ev.ConversacionId,
                EmisorId = ev.EmisorId,
                TipoMensajeId = tipoId,
                Contenido = ev.Contenido,
                CreadoAt = DateTime.UtcNow
            };

            _context.Mensajes.Add(nuevoMensaje);
            await _context.SaveChangesAsync();

            await _notificationRepository.PublicarNuevoMensajeAsync(new NuevoMensajeNotification(
                MensajeId: nuevoMensaje.Id,
                ConversacionId: nuevoMensaje.ConversacionId,
                EmisorId: nuevoMensaje.EmisorId,
                Contenido: nuevoMensaje.Contenido ?? string.Empty,
                TipoMensajeCodigo: ev.TipoMensajeCodigo,
                CreadoAt: nuevoMensaje.CreadoAt
            ));
        }

        public async Task ProcesarNuevoChatUsuarioOnEvent(IniciarChatIndividualEvent ev)
        {
           
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
            
                var conversacionExistenteId = await _context.ParticipantesConversacion
                    .Where(p => p.UsuarioId == ev.EmisorId || p.UsuarioId == ev.ReceptorId)
                    .GroupBy(p => p.ConversacionId)
                    .Where(g => g.Count() == 2) 
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                Guid chatId = conversacionExistenteId;

                if (chatId == Guid.Empty)
                {
                    chatId = Guid.NewGuid();

                    var nuevaConversacion = new Conversacion
                    {
                        Id = chatId,
                        EsGrupal = false,
                        CreadoAt = DateTime.UtcNow
                    };
                    _context.Conversaciones.Add(nuevaConversacion);

                    _context.ParticipantesConversacion.Add(new ParticipanteConversacion { ConversacionId = chatId, UsuarioId = ev.EmisorId, CreadoAt = DateTime.UtcNow });
                    _context.ParticipantesConversacion.Add(new ParticipanteConversacion { ConversacionId = chatId, UsuarioId = ev.ReceptorId, CreadoAt = DateTime.UtcNow });

                    await _context.SaveChangesAsync();


                    await _notificationRepository.PublicarChatCreadoAsync(new ChatCreadoNotification(
                        ConversacionId: chatId,
                        EsGrupal: false,
                        Nombre: null,
                        ParticipantesIds: new List<Guid> { ev.EmisorId, ev.ReceptorId },
                        CreadoAt: nuevaConversacion.CreadoAt
                    ));

                    var yaExisteMensaje = await _context.Mensajes.AnyAsync(m => m.Id == ev.MensajeId);
                    if (!yaExisteMensaje)
                    {
                        var tipoMensaje = await _context.TiposMensaje.FirstOrDefaultAsync(t => t.Codigo == ev.TipoMensajeCodigo);
                        int tipoId = tipoMensaje?.Id ?? 1;

                        var nuevoMensaje = new Mensaje
                        {
                            Id = ev.MensajeId, // ID del Frontend
                            ConversacionId = chatId,
                            EmisorId = ev.EmisorId,
                            TipoMensajeId = tipoId,
                            Contenido = ev.Contenido,
                            CreadoAt = DateTime.UtcNow
                        };

                        _context.Mensajes.Add(nuevoMensaje);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        await _notificationRepository.PublicarNuevoMensajeAsync(new NuevoMensajeNotification(
                            MensajeId: nuevoMensaje.Id,
                            ConversacionId: chatId,
                            EmisorId: nuevoMensaje.EmisorId,
                            Contenido: nuevoMensaje.Contenido ?? string.Empty,
                            TipoMensajeCodigo: ev.TipoMensajeCodigo,
                            CreadoAt: nuevoMensaje.CreadoAt
                        ));
                    }
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; 
            }
        }

        public async Task ProcesarLecturaMensajeOnEvent(ChatLeidoEvent ev)
        {
         
            var participante = await _context.ParticipantesConversacion
                .FirstOrDefaultAsync(p => p.ConversacionId == ev.ConversacionId && p.UsuarioId == ev.UsuarioId);

            if (participante != null)
            {

                participante.UltimoLeidoAt = ev.LeidoAt;
                await _context.SaveChangesAsync();

                await _notificationRepository.PublicarChatLeidoAsync(new ChatLeidoNotification(
                    ConversacionId: ev.ConversacionId,
                    UsuarioId: ev.UsuarioId,
                    LeidoAt: ev.LeidoAt
                ));
            }
        }
    }
}
