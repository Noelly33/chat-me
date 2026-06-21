using Consumer.Messaging.Worker.Domain.Entities;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Core.Mensajes.Api.Infrastructure.Repositories
{
    


    public class MensajesRepository : IMessageRepository
    {
        private readonly ChatDbContext _context;

        public MensajesRepository(ChatDbContext context)
        {
            _context = context;
        }
        public async Task<Mensaje> GetMensajeById(Guid mensajeId)
        {
            return await _context.Mensajes.FindAsync(mensajeId);
        }

        public async Task<List<Mensaje>> GetMensajesByConversacionId(Guid conversacionId, int pageNumber, int pageSize)
        {
            return await _context.Mensajes
                .Where(m => m.ConversacionId == conversacionId)
                .Include(m=> m.TipoMensaje)
                .Include(m => m.Emisor)
                .OrderByDescending(m => m.CreadoAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalMensajesByConversacionId(Guid conversacionId)
        {
            return await _context.Mensajes.CountAsync(m => m.ConversacionId == conversacionId);
        }
    }
}
