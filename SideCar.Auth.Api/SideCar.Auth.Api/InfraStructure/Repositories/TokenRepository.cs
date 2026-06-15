using Microsoft.EntityFrameworkCore;
using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.Domain.Repositories;
using SideCar.Auth.Api.InfraStructure.Context;

namespace SideCar.Auth.Api.InfraStructure.Repositories
{

    public class TokenRepository : ITokenRepository
    {
        public AuthContext _context;

        public TokenRepository(AuthContext context)
        {
            _context = context;
        }
        public async Task Add(RefreshToken token) => await _context.RefreshTokens.AddAsync(token);

        public async Task<RefreshToken?> FindByRefreshTokenAsync(string refreshToken)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.Usuario)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        }

        public void RevokeToken(RefreshToken token)
        {
            _context.RefreshTokens.Attach(token);
            token.EstaRevocado = true;
        }

        public async Task Save() => await _context.SaveChangesAsync();
    }
}
