using Consumer.Messaging.Worker.Domain.Entities;
using Consumer.Messaging.Worker.Domain.Events;
using Consumer.Messaging.Worker.Domain.Repositories;
using Consumer.Messaging.Worker.Infrastructure.Context;
using Consumer.Messaging.Worker.Infrastructure.Logging;
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
            MessageLogger.Log(ev.Seq, "← REPO.ProcesarMensajeOnEvent", new()
            {
                { "msgId", ev.MensajeId },
                { "conversacion", ev.ConversacionId },
                { "emisor", ev.EmisorId },
                { "tipo", ev.TipoMensajeCodigo },
            });

            try
            {
                var isExists = await _context.Mensajes.AnyAsync(m => m.Id == ev.MensajeId);
                MessageLogger.Log(ev.Seq, isExists ? "✓ DB.MSG_EXISTS_SKIP" : "→ DB.INSERT_MSG", new()
                {
                    { "msgId", ev.MensajeId },
                });
                if (isExists)
                {
                    MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarMensajeOnEvent_DONE");
                    return;
                }

                var tipoMensaje = await _context.TiposMensaje
                    .FirstOrDefaultAsync(t => t.Codigo == ev.TipoMensajeCodigo);

                int tipoId = tipoMensaje?.Id ?? 1;
                MessageLogger.Log(ev.Seq, "→ DB.LOOKUP_TIPO", new()
                {
                    { "codigo", ev.TipoMensajeCodigo },
                    { "tipoId", tipoId },
                    { "found", tipoMensaje != null },
                });

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
                MessageLogger.Log(ev.Seq, "✓ DB.INSERT_MSG_OK", new() { { "msgId", nuevoMensaje.Id } });

                await _notificationRepository.PublicarNuevoMensajeAsync(new NuevoMensajeNotification(
                    MensajeId: nuevoMensaje.Id,
                    ConversacionId: nuevoMensaje.ConversacionId,
                    EmisorId: nuevoMensaje.EmisorId,
                    Contenido: nuevoMensaje.Contenido ?? string.Empty,
                    TipoMensajeCodigo: ev.TipoMensajeCodigo,
                    CreadoAt: nuevoMensaje.CreadoAt
                ));
                MessageLogger.Log(ev.Seq, "✓ NOTIF.PublishNuevoMensaje", new() { { "msgId", nuevoMensaje.Id } });

                MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarMensajeOnEvent_DONE");
            }
            catch (Exception ex)
            {
                MessageLogger.Log(ev.Seq, "✗ REPO.ProcesarMensajeOnEvent_FAIL", new()
                {
                    { "msgId", ev.MensajeId },
                    { "error", ex.GetType().Name },
                    { "msg", ex.Message },
                });
                throw;
            }
        }

        public async Task ProcesarNuevoChatUsuarioOnEvent(IniciarChatIndividualEvent ev)
        {
            MessageLogger.Log(ev.Seq, "← REPO.ProcesarNuevoChatUsuarioOnEvent", new()
            {
                { "msgId", ev.MensajeId },
                { "emisor", ev.EmisorId },
                { "receptor", ev.ReceptorId },
            });

            using var transaction = await _context.Database.BeginTransactionAsync();
            MessageLogger.Log(ev.Seq, "→ TX.BEGIN");

            try
            {
                var conversacionExistenteId = await _context.ParticipantesConversacion
                    .Where(p => p.UsuarioId == ev.EmisorId || p.UsuarioId == ev.ReceptorId)
                    .GroupBy(p => p.ConversacionId)
                    .Where(g => g.Count() == 2)
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                Guid chatId = conversacionExistenteId;
                MessageLogger.Log(ev.Seq, chatId == Guid.Empty ? "→ DB.CONV_NOT_FOUND_NEW" : "✓ DB.CONV_EXISTS", new()
                {
                    { "chatId", chatId == Guid.Empty ? null : chatId.ToString() },
                });

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
                    MessageLogger.Log(ev.Seq, "✓ DB.CREATE_CONV_OK", new()
                    {
                        { "chatId", chatId },
                        { "emisor", ev.EmisorId },
                        { "receptor", ev.ReceptorId },
                    });

                    await _notificationRepository.PublicarChatCreadoAsync(new ChatCreadoNotification(
                        ConversacionId: chatId,
                        EsGrupal: false,
                        Nombre: null,
                        ParticipantesIds: new List<Guid> { ev.EmisorId, ev.ReceptorId },
                        CreadoAt: nuevaConversacion.CreadoAt
                    ));
                    MessageLogger.Log(ev.Seq, "✓ NOTIF.PublishChatCreado", new() { { "chatId", chatId } });

                    var yaExisteMensaje = await _context.Mensajes.AnyAsync(m => m.Id == ev.MensajeId);
                    if (yaExisteMensaje)
                    {
                        MessageLogger.Log(ev.Seq, "✓ DB.MSG_EXISTS_SKIP", new() { { "msgId", ev.MensajeId } });
                        await transaction.CommitAsync();
                        MessageLogger.Log(ev.Seq, "✓ TX.COMMIT");
                        MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarNuevoChatUsuarioOnEvent_DONE");
                        return;
                    }

                    var tipoMensaje = await _context.TiposMensaje.FirstOrDefaultAsync(t => t.Codigo == ev.TipoMensajeCodigo);
                    int tipoId = tipoMensaje?.Id ?? 1;
                    MessageLogger.Log(ev.Seq, "→ DB.LOOKUP_TIPO", new()
                    {
                        { "codigo", ev.TipoMensajeCodigo },
                        { "tipoId", tipoId },
                        { "found", tipoMensaje != null },
                    });

                    var nuevoMensaje = new Mensaje
                    {
                        Id = ev.MensajeId,
                        ConversacionId = chatId,
                        EmisorId = ev.EmisorId,
                        TipoMensajeId = tipoId,
                        Contenido = ev.Contenido,
                        CreadoAt = DateTime.UtcNow
                    };

                    _context.Mensajes.Add(nuevoMensaje);
                    await _context.SaveChangesAsync();
                    MessageLogger.Log(ev.Seq, "✓ DB.INSERT_MSG_OK", new() { { "msgId", nuevoMensaje.Id } });

                    await transaction.CommitAsync();
                    MessageLogger.Log(ev.Seq, "✓ TX.COMMIT");

                    await _notificationRepository.PublicarNuevoMensajeAsync(new NuevoMensajeNotification(
                        MensajeId: nuevoMensaje.Id,
                        ConversacionId: chatId,
                        EmisorId: nuevoMensaje.EmisorId,
                        Contenido: nuevoMensaje.Contenido ?? string.Empty,
                        TipoMensajeCodigo: ev.TipoMensajeCodigo,
                        CreadoAt: nuevoMensaje.CreadoAt
                    ));
                    MessageLogger.Log(ev.Seq, "✓ NOTIF.PublishNuevoMensaje", new() { { "msgId", nuevoMensaje.Id } });
                }

                MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarNuevoChatUsuarioOnEvent_DONE");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MessageLogger.Log(ev.Seq, "✗ REPO.ProcesarNuevoChatUsuarioOnEvent_FAIL", new()
                {
                    { "msgId", ev.MensajeId },
                    { "error", ex.GetType().Name },
                    { "msg", ex.Message },
                });
                throw;
            }
        }

        public async Task ProcesarLecturaMensajeOnEvent(ChatLeidoEvent ev)
        {
            MessageLogger.Log(ev.Seq, "← REPO.ProcesarLecturaMensajeOnEvent", new()
            {
                { "conversacion", ev.ConversacionId },
                { "usuario", ev.UsuarioId },
            });

            try
            {
                var participante = await _context.ParticipantesConversacion
                    .FirstOrDefaultAsync(p => p.ConversacionId == ev.ConversacionId && p.UsuarioId == ev.UsuarioId);

                if (participante == null)
                {
                    MessageLogger.Log(ev.Seq, "✗ DB.PARTICIPANTE_NOT_FOUND", new()
                    {
                        { "conversacion", ev.ConversacionId },
                        { "usuario", ev.UsuarioId },
                    });
                    MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarLecturaMensajeOnEvent_DONE");
                    return;
                }

                participante.UltimoLeidoAt = ev.LeidoAt;
                await _context.SaveChangesAsync();
                MessageLogger.Log(ev.Seq, "✓ DB.UPDATE_ULTIMO_LEIDO_OK", new()
                {
                    { "conversacion", ev.ConversacionId },
                    { "usuario", ev.UsuarioId },
                });

                await _notificationRepository.PublicarChatLeidoAsync(new ChatLeidoNotification(
                    ConversacionId: ev.ConversacionId,
                    UsuarioId: ev.UsuarioId,
                    LeidoAt: ev.LeidoAt
                ));
                MessageLogger.Log(ev.Seq, "✓ NOTIF.PublishChatLeido", new()
                {
                    { "conversacion", ev.ConversacionId },
                    { "usuario", ev.UsuarioId },
                });

                MessageLogger.Log(ev.Seq, "✓ REPO.ProcesarLecturaMensajeOnEvent_DONE");
            }
            catch (Exception ex)
            {
                MessageLogger.Log(ev.Seq, "✗ REPO.ProcesarLecturaMensajeOnEvent_FAIL", new()
                {
                    { "conversacion", ev.ConversacionId },
                    { "error", ex.GetType().Name },
                    { "msg", ex.Message },
                });
                throw;
            }
        }
    }
}
