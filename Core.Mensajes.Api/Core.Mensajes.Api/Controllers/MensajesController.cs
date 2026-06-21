using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Entities;
using Core.Mensajes.Api.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Core.Mensajes.Api.Controllers
{
    [ApiController]
    [Route("api/v1/messages")]
    public class MensajesController : Controller
    {
        private IMessagesService messagesService;

        public MensajesController(IMessagesService messagesService)
        {
            this.messagesService = messagesService;
        }


        [HttpGet]
        public async Task<ActionResult<MsResponse<PagedResponse<MessageReponseDTO>>>> GetMensajes([FromQuery] string conversationId, [FromQuery] string page, [FromQuery] string size)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return BadRequest(MsResponse<PagedResponse<MessageReponseDTO>>.Fail("Conversation ID is required"));
            }
            if (!int.TryParse(page, out int pageNumber) || pageNumber <= 0)
            {
                return BadRequest(MsResponse<PagedResponse<MessageReponseDTO>>.Fail("Invalid page number"));
            }
            if (!int.TryParse(size, out int pageSize) || pageSize <= 0)
            {
                return BadRequest(MsResponse<PagedResponse<MessageReponseDTO>>.Fail("Invalid page size"));
            }
            var result = await messagesService.GetMensajesByConversacionId(Guid.Parse(conversationId), pageNumber, pageSize);
            return Ok(result);
        }
    }
}
