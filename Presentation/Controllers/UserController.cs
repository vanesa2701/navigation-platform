using Application.Contracts;
using AutoMapper;
using Common.Exceptions;
using DTO.DTO.User;
using DTO.WebApiDTO.User;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Presentation.Extentions;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace Presentation.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUserServices _userServices;
        private IValidator<RegisterRequestDtoApi> _registerUserValidator;
        private IValidator<LoginRequestDtoApi> _loginUserValidator;
        public UserController(IMapper mapper, IUserServices userServices,
            IValidator<LoginRequestDtoApi> loginUserValidator, IValidator<RegisterRequestDtoApi> registerUserValidator)
        {
            _mapper = mapper;
            _userServices = userServices;
            _loginUserValidator = loginUserValidator;
            _registerUserValidator = registerUserValidator;
        }
        private Guid GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedException("Invalid or missing user id claim.");
            return Guid.Parse(id);
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(Tags = new[] { "Auth" })]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDtoApi request)
        {
            var result = await _registerUserValidator.ValidateAsync(request);

            if (!result.IsValid)
            {
                result.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(this.ModelState); result.AddToModelState(ModelState);
            }
            var response = await _userServices.RegisterUserAsync(_mapper.Map<RegisterRequestDto>(request));
            return Ok(_mapper.Map<RegisterResponseDtoApi>(response));
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(Tags = new[] { "Auth" })]
        public async Task<IActionResult> Login([FromBody] LoginRequestDtoApi request)
        {
            var result = await _loginUserValidator.ValidateAsync(request);

            if (!result.IsValid)
            {
                result.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(this.ModelState);
            }
            var response = await _userServices.LoginAsync(_mapper.Map<LoginRequestDto>(request));
            return Ok(response);
        }


        [Authorize]
        [HttpPost("logout")]
        [SwaggerOperation(Tags = new[] { "Auth" })]
        public async Task<IActionResult> Logout()
        {
            var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Authorization token is missing or invalid.");

            var token = auth.Substring("Bearer ".Length).Trim();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);

            await _userServices.LogoutAsync(token, userId, jti, HttpContext.RequestAborted);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/users/{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Administrator" })]
        public async Task<IActionResult> ChangeUserStatus(Guid id, [FromBody] AdminChangeUserStatusRequestDtoApi request)
        {
            var validator = HttpContext.RequestServices
                .GetRequiredService<FluentValidation.IValidator<AdminChangeUserStatusRequestDtoApi>>();

            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var adminUserId = GetCurrentUserId();

            await _userServices.ChangeUserStatusAsync(
                id,
                adminUserId,
                _mapper.Map<AdminChangeUserStatusRequestDto>(request));

            return NoContent();
        }
    }
}

