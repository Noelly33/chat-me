using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.Domain.Repositories
{
    public interface IAuthRepository
    {
        Task registerUser(Usuario usuario);
        Task Save();

        Task<Usuario?> GetUserByEmail(string email);
        Task<Usuario?> GetUserByUsername(string username);
        Task<Usuario?> GetUserById(Guid id);
    }
}
