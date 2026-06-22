using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideCar.Auth.Api.Domain.Services;
using SideCar.Auth.Api.DTOS;

namespace SideCar.Auth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private IValidator<RegisterUserDTO> _registerUserValidator;
        private IValidator<UpdateUserDTO> _updateUserValidator;
        private IValidator<LoginUserDTO> _loginUserValidator;
        private IAuthService _authService;
        private ITokenService _tokenService;


        public AuthController(
            IValidator<RegisterUserDTO> registerUserValidator,
            IValidator<UpdateUserDTO> updateUserValidator,
            IValidator<LoginUserDTO> loginUserValidator,
            IAuthService authService,
            ITokenService tokenService)
        {
            _registerUserValidator = registerUserValidator;
            _updateUserValidator = updateUserValidator;
            _loginUserValidator = loginUserValidator;
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<MsResponse<RegisterResultDTO>>> Register([FromBody] MsRequest<RegisterUserDTO> request)
        {
            var validationResult =  await _registerUserValidator.ValidateAsync(request.Data);
            if (!validationResult.IsValid)
            {
                return BadRequest(MsResponse<RegisterUserDTO>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }
            RegisterResultDTO result = await _authService.register(request.Data);
            return Ok(MsResponse<RegisterResultDTO>.Ok(result));
        }

        [HttpPost("login")]
        public async Task<ActionResult<MsResponse<LoginResultDTO>>> Login([FromBody] MsRequest<LoginUserDTO> request)
        {
            var validationResult = await _loginUserValidator.ValidateAsync(request.Data);
            if (!validationResult.IsValid)
            {
                return BadRequest(MsResponse<LoginUserDTO>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }
            try
            {
                var result = await _authService.Login(request.Data);
                return Ok(MsResponse<LoginResultDTO>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(MsResponse<LoginResultDTO>.Fail(ex.Message, "Login rechazado"));
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<MsResponse<TokenResponseDto>>> Refresh([FromBody] MsRequest<ResfreshTokenRequestDTO> request)
        {
            try
            {
                var result = await _tokenService.ResfreshToken(request.Data);
                return Ok(MsResponse<TokenResponseDto>.Ok(result));
            }
            catch (Microsoft.IdentityModel.Tokens.SecurityTokenException ex)
            {
                return Unauthorized(MsResponse<TokenResponseDto>.Fail(ex.Message, "Refresh token rechazado"));
            }
        }

        [HttpPost("validate")]
        public ActionResult<MsResponse<ValidateTokenResponseDTO>> Validate([FromBody] MsRequest<ValidateTokenRequestDTO> request)
        {
            var result = _tokenService.ValidateToken(request.Data.Token);
            if (result.Valid)
            {
                Response.Headers["X-User-Id"] = result.UserId?.ToString() ?? string.Empty;
                Response.Headers["X-Username"] = result.Username ?? string.Empty;
                Response.Headers["X-Email"] = result.Email ?? string.Empty;
                return Ok(MsResponse<ValidateTokenResponseDTO>.Ok(result));
            }
            return Unauthorized(MsResponse<ValidateTokenResponseDTO>.Fail(result.Reason ?? "Invalid token", "Token inválido"));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<MsResponse<UpdateUserResultDTO>>> UpdateProfile([FromBody] MsRequest<UpdateUserDTO> request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(MsResponse<UpdateUserResultDTO>.Fail("Token no contiene un userId válido"));
            }

            var validationResult = await _updateUserValidator.ValidateAsync(request.Data);
            if (!validationResult.IsValid)
            {
                return BadRequest(MsResponse<UpdateUserDTO>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            try
            {
                var result = await _authService.UpdateUser(userId, request.Data);
                return Ok(MsResponse<UpdateUserResultDTO>.Ok(result));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(MsResponse<UpdateUserResultDTO>.Fail(ex.Message));
            }
        }
    }
}
