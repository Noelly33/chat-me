
using Microsoft.EntityFrameworkCore;
using SideCar.Auth.Api.Domain.Model;
namespace SideCar.Auth.Api.InfraStructure.Context
{
    public class AuthContext :DbContext
    {
        public AuthContext(DbContextOptions<AuthContext> options): base(options)
        {

        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
