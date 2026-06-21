using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Domain.Services;

namespace Core.Mensajes.Api.Application
{
    public class MessageService : IMessagesService
    {
        private readonly IMessageRepository messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            this.messageRepository = messageRepository;
        }
        public async Task<MsResponse<PagedResponse<MessageReponseDTO>>> GetMensajesByConversacionId(Guid conversacionId, int pageNumber, int pageSize)
        {
            if(pageNumber<1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            int totalRecords = await messageRepository.GetTotalMensajesByConversacionId(conversacionId);

            var mensajes = await messageRepository.GetMensajesByConversacionId(conversacionId, pageNumber, pageSize);


            return new MsResponse<PagedResponse<MessageReponseDTO>>
            {
                Data = new PagedResponse<MessageReponseDTO>
                {
                    Data = mensajes.Select(m => new MessageReponseDTO
                    {
                        Id = m.Id,
                        TipoMensaje = m.TipoMensaje.Codigo,
                        Contenido = m.Contenido,
                        FechaCreacion = m.CreadoAt,
                        EmisorId = m.EmisorId,
                        EmisorNombre = m.Emisor.NombreUsuario
                    }).ToList(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                },
                Success = true
            };
        }
    }
}
