using Consumer.Messaging.Worker.Domain.Entities;

namespace Core.Mensajes.Api.Domain.Repositories
{
    public interface IMessageRepository
    {
        Task<List<Mensaje>> GetMensajesByConversacionId(Guid conversacionId, int pageNumber, int pageSize);

        Task<Mensaje> GetMensajeById(Guid mensajeId);

        Task<int> GetTotalMensajesByConversacionId(Guid conversacionId);
    }
}
