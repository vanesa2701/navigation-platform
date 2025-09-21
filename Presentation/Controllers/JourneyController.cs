using Application.Contracts;
using Application.Services.Messaging;
using AutoMapper;
using DTO.DTO.Journey;
using DTO.WebApiDTO.Journey;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extentions;
using Presentation.Utilities;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class JourneyController : ControllerBase
    {
        private readonly IJourneyServices _journeyServices;
        private readonly IMapper _mapper;
        private readonly IJWTUtilities _jwt;
        private readonly IValidator<JourneyShareRequestDtoApi> _journeyShareValidator;
        private readonly IValidator<AddJourneyRequestDtoApi> _addjourneyValidator;
        private readonly IValidator<JourneyFilterRequestDtoApi> _jounreyFilterValidator;
        private readonly IValidator<MonthlyRouteDistanceDtoApi> _monthlyJourniesValidator;
        private readonly IValidator<JourneyUnshareRequestDtoApi> _journeyUnshareValidator;

        public JourneyController(
            IJourneyServices journeyService,
            IMapper mapper,
            IJWTUtilities jwt,
            IValidator<JourneyShareRequestDtoApi> journeyShareValidator,
            IValidator<AddJourneyRequestDtoApi> addjourneyValidator,
            IValidator<JourneyFilterRequestDtoApi> jounreyFilterValidator,
            IValidator<MonthlyRouteDistanceDtoApi> monthlyJourniesValidator,
            IValidator<JourneyUnshareRequestDtoApi> journeyUnshareValidator)
        {
            _journeyServices = journeyService;
            _mapper = mapper;
            _jwt = jwt;
            _journeyShareValidator = journeyShareValidator;
            _addjourneyValidator = addjourneyValidator;
            _jounreyFilterValidator = jounreyFilterValidator;
            _monthlyJourniesValidator = monthlyJourniesValidator;
            _journeyUnshareValidator = journeyUnshareValidator;
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<ActionResult<Guid>> AddJourney([FromBody] AddJourneyRequestDtoApi request)
        {
            var validation = await _addjourneyValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var userId = GetCurrentUserId();

            var id = await _journeyServices.AddJourneyAsync(
                _mapper.Map<AddJourneyRequestDto>(request, opt => opt.AfterMap((_, dest) =>
                {
                    dest.UserId = userId;
                }))
            );

            return CreatedAtAction(nameof(GetJourneyById), new { id }, null);
        }

        [Authorize(Roles = "User")]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(JourneyDtoApi), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<ActionResult<JourneyDtoApi>> GetJourneyById(Guid id)
        {
            var journey = await _journeyServices.GetJourneyByIdAsync(id);
            return Ok(_mapper.Map<JourneyDtoApi>(journey));
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        [ProducesResponseType(typeof(List<JourneyDtoApi>), StatusCodes.Status200OK)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<ActionResult<List<JourneyDtoApi>>> GetAllJourneys()
        {
            var userId = GetCurrentUserId();
            var journeys = await _journeyServices.GetAllJourneysForUserAsync(userId);
            return Ok(_mapper.Map<List<JourneyDtoApi>>(journeys));
        }

        [Authorize(Roles = "User")]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> DeleteJourney(Guid id)
        {
            await _journeyServices.DeleteJourneyAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "User")]
        [HttpPost("{id:guid}/share")]
        [ProducesResponseType(typeof(JourneyShareResponseDtoApi), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> ShareJourney(Guid id, [FromBody] JourneyShareRequestDtoApi request)
        {
            var validation = await _journeyShareValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var userId = GetCurrentUserId();

            var result = await _journeyServices.ShareJourneyAsync(
                id,
                userId,
                _mapper.Map<JourneyShareRequestDto>(request));

            return Ok(_mapper.Map<JourneyShareResponseDtoApi>(result));
        }

        [Authorize(Roles = "User")]
        [HttpDelete("{id:guid}/share")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> UnshareJourney(Guid id, [FromBody] JourneyUnshareRequestDtoApi request)
        {
            var validation = await _journeyUnshareValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var userId = GetCurrentUserId();

            await _journeyServices.UnshareJourneyAsync(
                id,
                userId,
                _mapper.Map<DTO.DTO.Journey.JourneyUnshareRequestDto>(request));

            return NoContent();
        }

        [Authorize(Roles = "User")]
        [HttpPost("{id:guid}/public-link")]
        [ProducesResponseType(typeof(PublicJourneyLinkResponseDtoApi), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> GeneratePublicLink(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _journeyServices.GeneratePublicLinkAsync(id, userId);
            return Ok(_mapper.Map<PublicJourneyLinkResponseDtoApi>(result));
        }

        [Authorize(Roles = "User")]
        [HttpPut("{id:guid}/public-link")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> RevokePublicLink(Guid id)
        {
            var userId = GetCurrentUserId();
            await _journeyServices.RevokePublicLinkAsync(id, userId);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("public/{token}")]
        [ProducesResponseType(typeof(JourneyPublicLinkDtoApi), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [SwaggerOperation(Tags = new[] { "Users" })]
        public async Task<IActionResult> GetPublicJourneyByToken(string token)
        {
            var dto = await _journeyServices.GetPublicJourneyByTokenAsync(token);

            if (dto.IsRevoked)
                return StatusCode(StatusCodes.Status410Gone);

            return Ok(_mapper.Map<JourneyPublicLinkDtoApi>(dto));
        }

        // ---------- ADMIN ENDPOINTS ----------

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/journeys")]
        [ProducesResponseType(typeof(JourneyFilterResponseDtoApi), StatusCodes.Status200OK)]
        [SwaggerOperation(Tags = new[] { "Administrator" })]
        public async Task<IActionResult> GetJourneys([FromQuery] JourneyFilterRequestDtoApi filter)
        {
            var validation = await _jounreyFilterValidator.ValidateAsync(filter);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var result = await _journeyServices.GetJourniesByFilter(_mapper.Map<JourneyFilterRequestDto>(filter));

            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();

            return Ok(_mapper.Map<JourneyFilterResponseDtoApi>(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/monthlyRouteDistances")]
        [ProducesResponseType(typeof(List<MonthlyRouteDistanceResponseDtoApi>), StatusCodes.Status200OK)]
        [SwaggerOperation(Tags = new[] { "Administrator" })]
        public async Task<IActionResult> GetMonthlyRouteDistances([FromQuery] MonthlyRouteDistanceDtoApi filter)
        {
            var validation = await _monthlyJourniesValidator.ValidateAsync(filter);
            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState);
                ValidationExtensions.CheckModelState(ModelState);
            }

            var items = await _journeyServices.GetMonthlyDistancesAsync(
                _mapper.Map<MonthlyRouteDistanceDto>(filter));

            Response.Headers["X-Total-Count"] = items.Count.ToString();

            return Ok(_mapper.Map<List<MonthlyRouteDistanceResponseDtoApi>>(items));
        }
    }
}

