using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Core.Mensajes.Api.Controllers
{
    [ApiController]
    [Route("api/v1/conversations")]
    public class ConversacionesController : Controller
    {
        private IConversacionService conversacionService;

        public ConversacionesController(IConversacionService conversacionService)
        {
            this.conversacionService = conversacionService;
        }

        [HttpGet]
        public async Task<ActionResult<MsResponse<List<ConversacionListadoDTO>>>> GetConversaciones(
            [FromHeader(Name = "x-user-id")] string userId)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out _))
            {
                return BadRequest(MsResponse<List<ConversacionListadoDTO>>.Fail("User ID is required and must be a valid GUID"));
            }

            var result = await conversacionService.GetConversaciones(Guid.Parse(userId));
            return Ok(result);
        }
    }
}
