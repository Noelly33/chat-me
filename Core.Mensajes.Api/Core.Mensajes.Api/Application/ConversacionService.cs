using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Mensajes.Api.Domain.DTOS;
using Core.Mensajes.Api.Domain.Repositories;
using Core.Mensajes.Api.Domain.Services;

namespace Core.Mensajes.Api.Application
{
    public class ConversacionService : IConversacionService
    {
        private readonly IConversacionRepository _repository;

        public ConversacionService(IConversacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<MsResponse<List<ConversacionListadoDTO>>> GetConversaciones(Guid userId)
        {
            var conversaciones = await _repository.GetConversacionesForUserId(userId);
            return new MsResponse<List<ConversacionListadoDTO>>
            {
                Success = true,
                Data = conversaciones,
            };
        }
    }
}
