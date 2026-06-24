using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;

namespace Core.Mensajes.Api.Domain.Repositories
{
    public interface IContactoRepository
    {
        Task<List<ContactoListadoDTO>> GetContactosForUserId(Guid userId, int pageNumber, int pageSize);

        Task<Usuario> GetContactById(Guid userId);

        Task<int> GetTotalContactos(Guid userId);

        Task<List<ContactoResponseDTO>> SearchUsersByUsername(string query, Guid currentUserId, int maxResults);
    }
}
