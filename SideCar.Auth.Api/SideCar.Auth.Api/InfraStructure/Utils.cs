using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SideCar.Auth.Api.InfraStructure
{
    public static class Utils
    {

        public static Guid getGuidUserByExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null) throw new ArgumentException();

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
                              ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null) throw new InvalidOperationException();

            return Guid.Parse(userIdClaim.Value);
        }
    }
}
