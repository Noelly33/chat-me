using Microsoft.EntityFrameworkCore;
using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.Domain.Repositories;
using SideCar.Auth.Api.InfraStructure.Context;

namespace SideCar.Auth.Api.InfraStructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {

        public AuthContext _context;

        public AuthRepository(AuthContext context)
        {
            _context = context;
        }

        public async Task registerUser(Usuario usuario) => await _context.Usuarios.AddAsync(usuario);

        public async Task Save() => await _context.SaveChangesAsync();


        public async Task<Usuario?> GetUserByEmail(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> GetUserById(Guid id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario?> GetUserByUsername(string username)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == username);
        }
    }
}
