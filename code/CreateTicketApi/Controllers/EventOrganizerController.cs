using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventOrganizerController : ControllerBase
    {
        private readonly ILogger<EventOrganizerController> _logger;
        private readonly EventOrganizerDBAccess _organizerDbAccess;
        private readonly EventOrganizerMembersDbAccess _organizerMembersDbAccess;
        private readonly UserDbAccess _userDbAccess;
        private readonly JwtUtils _tokenUtils;
       private readonly IConfiguration _configuration;

        public EventOrganizerController(ILogger<EventOrganizerController> logger, 
                                    EventOrganizerDBAccess organizerDbAccess,
                                    EventOrganizerMembersDbAccess organizerMemberDbAccess,
                                    JwtUtils jwtUtils,
                                    UserDbAccess userDbAccess,
                                    IConfiguration configuration)
        {
            _logger = logger;
            _organizerDbAccess = organizerDbAccess;
            _organizerMembersDbAccess = organizerMemberDbAccess;
            _tokenUtils = jwtUtils;
            _configuration = configuration;
            _userDbAccess = userDbAccess;
        }

        [EnableRateLimiting("public-browsing")]
        [HttpGet("{id}")]
        public async Task<ActionResult<EventOrganizer>> GetById(int id)
        {
            var organizer = await _organizerDbAccess.GetOrganizerById(id);
            if (organizer == null)
                return NotFound();

            //resetting fields which do not make sense when not authenticated.
           // organizer.StripeAccountId = string.Empty;
          
            return organizer;
        }

        [EnableRateLimiting("public-browsing")]
        [HttpGet("ByName/{name}")]
        public async Task<ActionResult<EventOrganizer>> GetByEventBaseUrl(string name)
        {
            var organizer = await _organizerDbAccess.GetOrganizerByEventBaseUrl(name);
            if (organizer == null)
                return NotFound();
            return organizer;
        }
        
      
        [EnableRateLimiting("strict-ip-auth")]
        [HttpGet("CheckUniqueOrgName/{orgName}")]
        [Authorize]
        public async Task<ActionResult<bool>> CheckUniqueOrgName(string orgName)=> 
                    !_organizerDbAccess.GetAllOrgNames().Result.Any(s=>string.Equals(StringUtils.CreateUrlSlug(s),orgName,StringComparison.OrdinalIgnoreCase));

        [EnableRateLimiting("strict-ip-auth")]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<int>> Add([FromBody] EventOrganizer organizer)
        {
            if (organizer == null)
                return BadRequest("Invalid organizer.");
            string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (role != UserRoles.Attendee.ToString())
            {
                return BadRequest($"Logged in role is incorrect in order to become an organizer: {role}");
            }
            int customerId = int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
            if (customerId != 0)
            {
                return BadRequest($"Logged in user is already member of an organization");
            }
          
            int userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            EventUser user = await _userDbAccess.GetUserById(userId);
            if (user == null)
            {
                return NotFound("Unable to locate user sent");
            }

            if (string.IsNullOrEmpty(organizer.OrganizationName) || string.IsNullOrEmpty(organizer.OrganizerAboutMe) 
                    || string.IsNullOrEmpty(organizer.OrganizerEmail) || string.IsNullOrEmpty(organizer.OrganizerDescription) ||
                    string.IsNullOrEmpty(organizer.OrganizerEventBaseUrl))
            {
                return BadRequest("Incomplete organizer data for setup");
            }   

            customerId = await _organizerDbAccess.AddOrganizer(organizer);
            if (customerId > 0)
            {
                _logger.LogInformation($"Added to customer table and generated customer id {customerId} for logged in user {userId} and name {user.Name}");
                EventOrganizerMembers member = new EventOrganizerMembers()
                {
                    UserId = userId,
                    CustomerId = customerId,
                    IsActive= true,
                    Role = UserRoles.Owner.ToString()
                };
                (int memberId,string token) = await _organizerMembersDbAccess.AddMember(member);
                if (memberId >0)
                {
                    _logger.LogInformation($"Added user {userId} as owner of org {customerId}");
                    
                    var refreshToken = Request.Cookies["refreshToken"];
                  
                    if (string.IsNullOrWhiteSpace(refreshToken) || !await _tokenUtils.ValidateJwtToken(refreshToken))
                    {
                        _logger.LogInformation($"invalid refresh token in env: {refreshToken} {_configuration["HostEnvironment:Name"]}");
                        //in dev since we are testing with 2 different tunnels(domains)
                        //one for api and and for web, there is no cookie being sent by
                        //the browser on iphone. 
                        //Hence this always fails and logs the user out.
                        if (_configuration["HostEnvironment:Name"] == "Production")
                            return Unauthorized("Invalid refresh token.");
                    }
                    else
                    {
                        await _tokenUtils.RevokeTokenInCache(refreshToken);
                    }

                    var newRefreshToken = await _tokenUtils.GenerateRefreshToken(userId.ToString(), UserRoles.Owner.ToString(), customerId);      
                    SetSecureCookie("refreshToken", newRefreshToken);
                    var newAccessToken = _tokenUtils.GenerateJwtToken(userId.ToString(), UserRoles.Owner.ToString(), customerId);
                    return Ok(new { accessToken = newAccessToken,
                                    user = new { id = userId, email = user.Email, role = UserRoles.Owner.ToString(), customerId = customerId, 
                                            name = user.Name ?? string.Empty } });
                }
                else
                {
                    return StatusCode(500,$"Error adding user with name {user.Name} with role owner to Member table");
                }
            }
            else
            {
                return  StatusCode(500,"Error adding member as owner");
            }
        
        }

        private void SetSecureCookie(string name, string value)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps, // use secure only when request is over HTTPS; allows local HTTP development
                //for cross site mobile access, need to set SameSiteMode.None and set Secure=true in production
                SameSite = SameSiteMode.None,
                Path = "/"
            };
            Response.Cookies.Append(name, value, options);
        }

        [EnableRateLimiting("strict-ip-auth-organizer")]
        [HttpPut("{customerId}")]
        [Authorize(Policy = "MatchingCustomer")]
        [Authorize(Policy = "FullAdminMinimum")]
        public async Task<IActionResult> Update(int customerId, [FromBody] EventOrganizer organizer)
        {
            if (organizer == null || customerId != organizer.OrganizerId)
                return BadRequest("Invalid organizer or ID mismatch.");
            var result = await _organizerDbAccess.UpdateOrganizer(organizer);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update organizer.");
        }

        [HttpDelete("{customerId}")]
        [Authorize(Policy = "MatchingCustomer")]
        [Authorize(Policy = "OwnerOnly")]
        public async Task<IActionResult> Delete(int customerId)
        {
            var result = await _organizerDbAccess.DeleteOrganizer(customerId);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete organizer.");
        }
    }
}