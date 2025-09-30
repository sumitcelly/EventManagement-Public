using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserDbAccess _userDbAccess;
        private readonly EventOrganizerMembersDbAccess _eventOrganizerMembersDbAccess;

        private readonly JwtUtils _tokenUtils;

        public UserController(ILogger<UserController> logger, UserDbAccess userDbAccess,
                EventOrganizerMembersDbAccess eventOrganizerMembersDbAccess, JwtUtils tokenUtils)
        {
            _logger = logger;
            _userDbAccess = userDbAccess;
            _eventOrganizerMembersDbAccess = eventOrganizerMembersDbAccess;
            _tokenUtils = tokenUtils;
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<EventUser>> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is null or empty.");
            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null)
                return NotFound();
            return user;
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginRequest request)
        {
            string email = request.Email;
            
            string password = request.Password;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return BadRequest("Email or password is null or empty.");
            var user = await _userDbAccess.GetUserByEmailAndPassword(email, password);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            //Get role for the user
            EventOrganizerMembers orgMember = await _eventOrganizerMembersDbAccess.GetContainingOrgByUserId(user.UserId);
            if (orgMember == null)
                _logger.LogInformation($"User with email {user.Email} is not part of any organization.  Returning default role.");
            string role = orgMember?.Role ?? UserRoles.Attendee.ToString(); // Default to "User" if no organization member found
            _logger.LogInformation($"User {email} logged in with role {role}.");


            //create a JWT token or session here as needed
            var _accessToken = _tokenUtils.GenerateJwtToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0);
            string refreshToken = _tokenUtils.GenerateRefreshToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0);
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });
            return Ok(new
            {
                accessToken = _accessToken,
                user = new { id = user.UserId, email = request.Email, role = role ?? null, customerId = orgMember?.CustomerId ?? 0 }
            });
        }

          [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            //need to store refresh token in db or redis so it can be revoked if compromised
            //so basically token/userid store to validate this.
            if (string.IsNullOrEmpty(refreshToken) /*|| !RefreshTokens.ContainsKey(refreshToken)*/)
                return Unauthorized();


            if (!await _tokenUtils.ValidateJwtToken(refreshToken))
                return Unauthorized("Invalid refresh token.");

            //var userId = RefreshTokens[refreshToken];
            var claims = _tokenUtils.GetClaimsFromToken(refreshToken);
            string userId = claims.Item1;
            string role = claims.Item2;
            string customerId = claims.Item3;
            
            //when stored in db, no need to store userId, role, customerId in token
            var newRefreshToken = _tokenUtils.GenerateRefreshToken(userId.ToString(), role, Convert.ToInt16(customerId));
            //RefreshTokens.Remove(refreshToken);
            //RefreshTokens[newRefreshToken] = userId;

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            var newAccessToken = _tokenUtils.GenerateJwtToken(userId, role, Convert.ToInt16(customerId));

            return Ok(new { accessToken = newAccessToken });
        }

           [HttpPost("logout")]
        public IActionResult Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                //RefreshTokens.Remove(refreshToken);
                Response.Cookies.Delete("refreshToken");
            }

            return Ok(new { message = "Logged out" });
        }
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] EventUser user)
        {
            if (user == null)
                return BadRequest("Invalid user.");
            var id = await _userDbAccess.CreateUser(user);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create user.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EventUser user)
        {
            if (user == null || id != user.UserId)
                return BadRequest("Invalid user or ID mismatch.");
            var result = await _userDbAccess.UpdateUser(user);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update user.");
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> Delete(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Invalid user ID.");
            var result = await _userDbAccess.DeleteUserByEmail(email);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete user.");
        }
    }
    
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}