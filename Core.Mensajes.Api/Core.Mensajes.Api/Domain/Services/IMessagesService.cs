using Consumer.Messaging.Worker.Domain.Entities;
using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;

namespace Core.Mensajes.Api.Domain.Services
{
    public interface IMessagesService
    {
        public Task<MsResponse<PagedResponse<MessageReponseDTO>>> GetMensajesByConversacionId(Guid conversacionId, int pageNumber, int pageSize); 
    }
}
