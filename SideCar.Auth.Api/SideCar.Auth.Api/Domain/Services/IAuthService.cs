using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.Domain.Services
{
    public interface IAuthService
    {
        Task<RegisterResultDTO> register(RegisterUserDTO request);
        Task<UpdateUserResultDTO> UpdateUser(Guid userId, UpdateUserDTO request);
        Task<LoginResultDTO> Login(LoginUserDTO request);
    }
}
