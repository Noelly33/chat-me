using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Core.Mensajes.Api.Infrastructure.Repositories
{
    public class ConversacionRepository : IConversacionRepository
    {
        private readonly ChatDbContext _context;

        public ConversacionRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConversacionListadoDTO>> GetConversacionesForUserId(Guid userId)
        {
            var intermedios = await _context.ParticipantesConversacion
                .Where(pc => pc.UsuarioId == userId)
                .Select(pc => new
                {
                    ConversacionId = pc.ConversacionId,
                    UltimaFecha = pc.Conversacion.Mensajes.Max(m => (DateTime?)m.CreadoAt) ?? DateTime.MinValue,
                    OtroUsuario = pc.Conversacion.Participantes
                        .Where(opc => opc.UsuarioId != userId)
                        .Select(opc => opc.Usuario)
                        .FirstOrDefault(),
                    CreadoAt = pc.Conversacion.CreadoAt,
                })
                .Where(x => x.OtroUsuario != null)
                .OrderByDescending(x => x.UltimaFecha)
                .AsNoTracking()
                .ToListAsync();

            if (!intermedios.Any())
            {
                return new List<ConversacionListadoDTO>();
            }

            var conversacionIds = intermedios.Select(x => x.ConversacionId).Distinct().ToList();

            var mensajesPorConv = await _context.Mensajes
                .Where(m => conversacionIds.Contains(m.ConversacionId))
                .GroupBy(m => m.ConversacionId)
                .Select(g => new
                {
                    ConversacionId = g.Key,
                    Ultimo = g.OrderByDescending(m => m.CreadoAt)
                        .Select(m => new MensajePreviewDTO
                        {
                            Id = m.Id,
                            Contenido = m.Contenido,
                            FechaCreacion = m.CreadoAt,
                            TipoMensaje = m.TipoMensaje.Codigo,
                            EmisorId = m.EmisorId,
                        })
                        .FirstOrDefault(),
                })
                .AsNoTracking()
                .ToListAsync();

            var mensajesDict = mensajesPorConv.ToDictionary(x => x.ConversacionId, x => x.Ultimo);

            return intermedios.Select(x => new ConversacionListadoDTO
            {
                Id = x.ConversacionId,
                CreadoAt = x.CreadoAt,
                OtroUsuario = new ContactoResponseDTO
                {
                    Id = x.OtroUsuario!.Id,
                    NombreUsuario = x.OtroUsuario.NombreUsuario,
                    Email = x.OtroUsuario.Email,
                    Nombres = x.OtroUsuario.Nombres,
                    Apellidos = x.OtroUsuario.Apellidos,
                    FechaNacimiento = x.OtroUsuario.FechaNacimiento,
                    NumeroTelefono = x.OtroUsuario.NumeroTelefono,
                    AvatarUrl = x.OtroUsuario.AvatarUrl,
                    CreadoAt = x.OtroUsuario.CreadoAt,
                },
                UltimoMensaje = mensajesDict.TryGetValue(x.ConversacionId, out var msg) ? msg : null,
            }).ToList();
        }
    }
}
