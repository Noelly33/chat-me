using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Domain.Services;

namespace Core.Mensajes.Api.Application
{
    public class ContactoService : IContactoService
    {
        private readonly IContactoRepository contactoRepository;

        public ContactoService(IContactoRepository contactoRepository)
        {
            this.contactoRepository = contactoRepository;
        }

        public async Task<MsResponse<PagedResponse<ContactoListadoDTO>>> GetContactos(Guid userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            int totalRecords = await contactoRepository.GetTotalContactos(userId);

            var contactos = await contactoRepository.GetContactosForUserId(userId, pageNumber, pageSize);

            return new MsResponse<PagedResponse<ContactoListadoDTO>>
            {
                Data = new PagedResponse<ContactoListadoDTO>
                {
                    Data = contactos,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                },
                Success = true
            };
        }

        public async Task<MsResponse<ContactoResponseDTO>> GetContactoById(Guid userId)
        {
            var contacto = await contactoRepository.GetContactById(userId);

            if (contacto == null)
            {
                return new MsResponse<ContactoResponseDTO>
                {
                    Success = false,
                    Message = "Contacto no encontrado",
                    Errors = new List<string> { $"No se encontró un contacto con el id {userId}" }
                };
            }

            return new MsResponse<ContactoResponseDTO>
            {
                Success = true,
                Data = new ContactoResponseDTO
                {
                    Id = contacto.Id,
                    NombreUsuario = contacto.NombreUsuario,
                    Email = contacto.Email,
                    Nombres = contacto.Nombres,
                    Apellidos = contacto.Apellidos,
                    FechaNacimiento = contacto.FechaNacimiento,
                    NumeroTelefono = contacto.NumeroTelefono,
                    AvatarUrl = contacto.AvatarUrl,
                    CreadoAt = contacto.CreadoAt
                }
            };
        }
    }
}
