using SideCar.Auth.Api.Domain.Model;

namespace SideCar.Auth.Api.Domain.Repositories
{
    public interface ITokenRepository
    {
        Task Add(RefreshToken token);
        Task Save();

        Task<RefreshToken?> FindByRefreshTokenAsync(string refreshToken);

        void RevokeToken(RefreshToken token);
    }
}
