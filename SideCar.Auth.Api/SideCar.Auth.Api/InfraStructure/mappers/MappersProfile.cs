using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SideCar.Auth.Api.Domain.Model;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.InfraStructure.mappers
{
    public class MappersProfile:Profile
    {
        private readonly PasswordHasher<Usuario> _passwordHasher = new();

        public MappersProfile()
        {
            CreateMap<RegisterUserDTO, Usuario>()

            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))

            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src =>
                _passwordHasher.HashPassword(new Usuario(), src.Password)))

            .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src =>
                src.FechaNacimiento == DateTime.MinValue || src.FechaNacimiento == null
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(src.FechaNacimiento.Value, DateTimeKind.Utc)))

            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => (string?)null))

            .ForMember(dest => dest.CreadoAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.RefreshTokens, opt => opt.MapFrom(src => new List<RefreshToken>()));
        }
    }
}
