using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;
using Core.Mensajes.Api.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Core.Mensajes.Api.Controllers
{
    [ApiController]
    [Route("api/v1/contacts")]
    public class ContactosController : Controller
    {
        private IContactoService contactoService;

        public ContactosController(IContactoService contactoService)
        {
            this.contactoService = contactoService;
        }

        [HttpGet]
        public async Task<ActionResult<MsResponse<PagedResponse<ContactoListadoDTO>>>> GetContactos(
            [FromHeader(Name = "x-user-id")] string userId,
            [FromQuery] string page,
            [FromQuery] string size)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out _))
            {
                return BadRequest(MsResponse<PagedResponse<ContactoListadoDTO>>.Fail("User ID is required and must be a valid GUID"));
            }
            if (!int.TryParse(page, out int pageNumber) || pageNumber <= 0)
            {
                return BadRequest(MsResponse<PagedResponse<ContactoListadoDTO>>.Fail("Invalid page number"));
            }
            if (!int.TryParse(size, out int pageSize) || pageSize <= 0)
            {
                return BadRequest(MsResponse<PagedResponse<ContactoListadoDTO>>.Fail("Invalid page size"));
            }

            var result = await contactoService.GetContactos(Guid.Parse(userId), pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MsResponse<ContactoResponseDTO>>> GetContactoById( Guid id)
        {
            var result = await contactoService.GetContactoById(id);

            if (!result.Success || result.Data == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<MsResponse<List<ContactoResponseDTO>>>> Search(
            [FromHeader(Name = "x-user-id")] string userId,
            [FromQuery] string query,
            [FromQuery] string size)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out _))
            {
                return BadRequest(MsResponse<List<ContactoResponseDTO>>.Fail("User ID is required and must be a valid GUID"));
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(MsResponse<List<ContactoResponseDTO>>.Fail("Query is required"));
            }

            int maxResults = 20;
            if (!string.IsNullOrEmpty(size) && int.TryParse(size, out var parsed) && parsed > 0)
            {
                maxResults = parsed;
            }

            var result = await contactoService.SearchUsuarios(Guid.Parse(userId), query, maxResults);
            return Ok(result);
        }
    }
}
