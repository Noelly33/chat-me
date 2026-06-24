using System;
using System.Collections.Generic;
using Core.Mensajes.Api.Domain.DTOS;

namespace Core.Mensajes.Api.Domain.Repositories
{
    public interface IConversacionRepository
    {
        Task<List<ConversacionListadoDTO>> GetConversacionesForUserId(Guid userId);
    }
}
