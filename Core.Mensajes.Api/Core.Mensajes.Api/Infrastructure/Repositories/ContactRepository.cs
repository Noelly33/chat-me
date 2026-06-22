using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Core.Mensajes.Api.Infrastructure.Repositories
{
    public class ContactRepository : IContactoRepository
    {
        private readonly ChatDbContext _context;

        public ContactRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<List<ContactoListadoDTO>> GetContactosForUserId(Guid userId, int pageNumber, int pageSize)
        {
            var intermedios = await _context.ParticipantesConversacion
                 .Where(pc => pc.UsuarioId == userId)
                 .Select(pc => new
                 {
                     ConversacionId = pc.ConversacionId,
                     UltimaFecha = pc.Conversacion.Mensajes.Max(m => (DateTime?)m.CreadoAt) ?? DateTime.MinValue,
                     Contacto = pc.Conversacion.Participantes
                        .Where(opc => opc.UsuarioId != userId)
                        .Select(opc => opc.Usuario)
                        .FirstOrDefault()
                 })
                 .Where(x => x.Contacto != null)
                 .OrderByDescending(x => x.UltimaFecha)
                 .Skip((pageNumber - 1) * pageSize)
                 .Take(pageSize)
                 .AsNoTracking()
                 .ToListAsync();

            if (!intermedios.Any())
                return new List<ContactoListadoDTO>();

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
                            EmisorId = m.EmisorId
                        })
                        .FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync();

            var mensajesDict = mensajesPorConv.ToDictionary(x => x.ConversacionId, x => x.Ultimo);

            return intermedios.Select(x => new ContactoListadoDTO
            {
                Id = x.Contacto!.Id,
                NombreUsuario = x.Contacto.NombreUsuario,
                Email = x.Contacto.Email,
                Nombres = x.Contacto.Nombres,
                Apellidos = x.Contacto.Apellidos,
                FechaNacimiento = x.Contacto.FechaNacimiento,
                NumeroTelefono = x.Contacto.NumeroTelefono,
                AvatarUrl = x.Contacto.AvatarUrl,
                CreadoAt = x.Contacto.CreadoAt,
                UltimoMensaje = mensajesDict.TryGetValue(x.ConversacionId, out var msg) ? msg : null
            }).ToList();
        }

        public async Task<int> GetTotalContactos(Guid userId)
        {
            var myConversations = await _context.ParticipantesConversacion.
               Where(pc => pc.UsuarioId == userId)
               .Select(pc => pc.ConversacionId)
               .ToListAsync();
            return await _context.ParticipantesConversacion.
                Where(pc => myConversations.Contains(pc.ConversacionId) && pc.UsuarioId != userId)
                .Select(pc => pc.UsuarioId)
                .Distinct()
                .CountAsync();
        }

        public async Task<Usuario> GetContactById(Guid userId)
        {
            return await _context.Usuarios.FindAsync(userId);

        }
    }
}
