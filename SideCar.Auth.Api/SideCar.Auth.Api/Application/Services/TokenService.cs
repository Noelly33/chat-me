using Microsoft.IdentityModel.Tokens;
using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.Domain.Repositories;
using SideCar.Auth.Api.Domain.Services;
using SideCar.Auth.Api.DTOS;
using SideCar.Auth.Api.InfraStructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SideCar.Auth.Api.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private ITokenRepository _tokenRepository;


        public TokenService(IConfiguration configuration,ITokenRepository tokenRepository)
        {
            _config = configuration;
            _tokenRepository = tokenRepository;
        }

        public async Task<TokenResponseDto> GenerarTokens(RegisterUserDTO usuario, Guid usuarioId)
        {
            var claims = new[]
            {
               new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("Nombres", usuario.Nombres),
            new Claim("Apellidos", usuario.Apellidos)
            };

            var secretKey = _config["JwtSettings:Secret"] ?? throw new ArgumentNullException("JWT Secret no configurado");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var minutosExpiracion = double.Parse(_config["JwtSettings:AccessTokenExpirationInMinutes"] ?? "15");
            var diasExpiracionRt = double.Parse(_config["JwtSettings:RefreshTokenExpirationInDays"] ?? "7");

            var tokenOptions = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutosExpiracion),
            signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            var fechaExpiracionRt = DateTime.UtcNow.AddDays(diasExpiracionRt);
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Token = GenerarCadenaAleatoriaSegura(),
                FechaExpiracion = fechaExpiracionRt,
                EstaRevocado = false,
                CreadoAt = DateTime.UtcNow
            };
            await _tokenRepository.Add(refreshTokenEntity);
            await _tokenRepository.Save();
            return new TokenResponseDto(accessToken, refreshTokenEntity.Token, refreshTokenEntity.FechaExpiracion);

        }

        public async Task<TokenResponseDto> ResfreshToken(ResfreshTokenRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new SecurityTokenException("Token y refresh token son requeridos");
            }

            Guid userId = Utils.getGuidUserByExpiredToken(request.Token);
            var refreshToken = await _tokenRepository.FindByRefreshTokenAsync(request.RefreshToken);
            if (refreshToken == null || refreshToken.EstaRevocado || refreshToken.FechaExpiracion <= DateTime.UtcNow)
            {
                throw new SecurityTokenException("Refresh token inválido o expirado");
            }
            if (refreshToken.Usuario.Id != userId)
            {
                _tokenRepository.RevokeToken(refreshToken);
                await _tokenRepository.Save();
                throw new SecurityTokenException("Refresh token no pertenece al usuario del token de acceso");
            }

            var usuario = refreshToken.Usuario;
            _tokenRepository.RevokeToken(refreshToken);

            var registerDto = new RegisterUserDTO
            {
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email,
                NombreUsuario = usuario.NombreUsuario,
                NumeroTelefono = usuario.NumeroTelefono ?? string.Empty,
                FechaNacimiento = usuario.FechaNacimiento ?? DateTime.MinValue,
                Password = string.Empty
            };

            return await GenerarTokens(registerDto, usuario.Id);
        }

        public ValidateTokenResponseDTO ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new ValidateTokenResponseDTO { Valid = false, Reason = "Token is required" };
            }

            var secretKey = _config["JwtSettings:Secret"] ?? throw new ArgumentNullException("JWT Secret no configurado");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["JwtSettings:Issuer"],
                    ValidAudience = _config["JwtSettings:Audience"],
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var usernameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;
                var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;

                return new ValidateTokenResponseDTO
                {
                    Valid = true,
                    UserId = Guid.TryParse(userIdClaim, out var id) ? id : null,
                    Username = usernameClaim,
                    Email = emailClaim,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new ValidateTokenResponseDTO { Valid = false, Reason = "Token expired" };
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return new ValidateTokenResponseDTO { Valid = false, Reason = "Invalid signature" };
            }
            catch (SecurityTokenException ex)
            {
                return new ValidateTokenResponseDTO { Valid = false, Reason = ex.Message };
            }
            catch (Exception)
            {
                return new ValidateTokenResponseDTO { Valid = false, Reason = "Invalid token" };
            }
        }

        private string GenerarCadenaAleatoriaSegura()
        {
            var numeroAleatorio = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(numeroAleatorio);
            return Convert.ToBase64String(numeroAleatorio);
        }
    }
}
