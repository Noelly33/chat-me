using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;

namespace Core.Mensajes.Api.Domain.Services
{
    public interface IContactoService
    {
        Task<MsResponse<PagedResponse<ContactoListadoDTO>>> GetContactos(Guid userId, int pageNumber, int pageSize);

        Task<MsResponse<ContactoResponseDTO>> GetContactoById(Guid userId);
    }
}
