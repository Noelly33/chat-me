using System;
using System.Collections.Generic;
using Core.Mensajes.Api.Domain.DTOS;

namespace Core.Mensajes.Api.Domain.Services
{
    public interface IConversacionService
    {
        Task<MsResponse<List<ConversacionListadoDTO>>> GetConversaciones(Guid userId);
    }
}
