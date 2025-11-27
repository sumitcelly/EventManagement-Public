using Amazon.S3.Model;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
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

        private readonly LoginCodesDbAccess _loginCodesDbAccess;
        private readonly JwtUtils _tokenUtils;

        private readonly SQSHelper _sqsHelper ;

        private readonly NotificationTemplateAccess _templateDbAccess;
        public UserController(ILogger<UserController> logger, UserDbAccess userDbAccess,
                EventOrganizerMembersDbAccess eventOrganizerMembersDbAccess, JwtUtils tokenUtils,
                LoginCodesDbAccess loginCodesDbAccess,
                SQSHelper sqsHelper,
                NotificationTemplateAccess templateDbAccess)
        {
            _logger = logger;
            _userDbAccess = userDbAccess;
            _eventOrganizerMembersDbAccess = eventOrganizerMembersDbAccess;
            _tokenUtils = tokenUtils;
            _loginCodesDbAccess = loginCodesDbAccess;
            _sqsHelper = sqsHelper;
            _templateDbAccess = templateDbAccess;
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
            EventOrganizerMembers? orgMember = await _eventOrganizerMembersDbAccess.GetContainingOrgByUserId(user.UserId);
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

        [Authorize]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] LoginRequest request)
        {
            string email = request.Email;
            string newPassword = request.Password;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword))
                return BadRequest("Email or new password is null or empty.");
            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null)
                return NotFound("User not found.");

            var result = await  _userDbAccess.ResetPassword(user.UserId, newPassword);
            if (result)
                return Ok("Password reset successful.");
            return StatusCode(500, "Failed to reset password.");
        }

        [HttpPost("VerifyEmailCode")]
        public async Task<IActionResult> VerifyEmailCode([FromBody] LoginRequest request)
        {
            string email = request.Email;
            string code = request.Password; //using password field to pass code
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
                return BadRequest("Email or code is null or empty.");
           
            var emailAddress = await _loginCodesDbAccess.GetEmailAddressByCode(code);
            if (string.IsNullOrWhiteSpace(emailAddress) || emailAddress != request.Email)
                return Unauthorized("Invalid code.");
            //code is valid, delete all codes for the user
            await _loginCodesDbAccess.UpdateUsedAt(code);

            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null && request.Signup.HasValue && !request.Signup.Value)
                return NotFound("User not found and no signup requested");
            if (user == null && request.Signup.HasValue && request.Signup.Value)
            {
                _logger.LogInformation($"User with email {email} not found, continuing signup by creating user.");
                //create a temporary user record for signup
                user = new EventUser
                {
                    Email = email,
                    Name = email.Split('@')[0],               
                };
                var id = await _userDbAccess.CreateUser(user);
                if (id <= 0)
                    return StatusCode(500, "Failed to create user during signup.");
                user.UserId = id;
            }
             //Get role for the user
            EventOrganizerMembers? orgMember = await _eventOrganizerMembersDbAccess.GetContainingOrgByUserId(user.UserId);
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

     
        [HttpGet("CheckUserExists/{email}")]
        public async Task<bool> CheckUserExists(string email)
        {
            var user =  await _userDbAccess.GetUserByEmail(email);
            return user != null;
        }

        [HttpGet("GenerateEmailCode/{email}/{signup?}")]
        public async Task<ActionResult<string>> GenerateEmailCode(string email, bool? signup = false)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is null or empty.");
            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null && signup != true)
                return NotFound();
            
            if (user == null && signup == true)
            {
                _logger.LogInformation($"User with email {email} not found, continuing since this is signup.");
               //create a temporary user record for signup
            }
            if (user != null && signup == true)
            {
                _logger.LogInformation($"User {email} already exists and tryig to singup, sending them one time code.");
               //create a temporary user record for signup
            }

            string emailCode= EventUtils.PasswordGenerator.GetPassword();
            var code = await _loginCodesDbAccess.CreateLoginCode(new LoginCode
            {
                EmailAddress = email,
                SecurityCode =emailCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RequestIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            if (code <= 0)
                return StatusCode(500, "Failed to generate email verification code.");
            
            Tuple<string,string> templateData = await _templateDbAccess.GetTemplateByName("EmailVerification");
            if (templateData == null || string.IsNullOrEmpty(templateData.Item1))
                return StatusCode(500, "Email template not found.");

            EmailTokenReplacement tokenReplacer = new EmailTokenReplacement();
            var values = new Dictionary<string, string>
            {
                { "EmailCode", emailCode },
                {"Attendee", user?.Name ?? "User" }
            };

            string content = tokenReplacer.ReplaceTokens(templateData.Item1, values);
            await _sqsHelper.QueueMessage(email,
                user?.Name ?? "User",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                templateData.Item2);

            return Ok("Email verification code sent.");
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

        [HttpPut("/user/{email}")]
        public async Task<IActionResult> Update(string email, [FromBody] EventUser user)
        {
            if (user == null || string.IsNullOrEmpty(email) || user.Email != email)
                return BadRequest("Invalid user or email mismatch.");
            var result = await _userDbAccess.UpdateUserByEmail(user);
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

   
        [Authorize]
        [HttpPost("UserSignup")]
        public async Task<ActionResult<int>> UserSignup([FromBody] EventUser user)
        {
            //Authorize because user email must be verified before signup
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Invalid user.");
    
            var id = await _userDbAccess.CreateUser(user);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create user.");
        }
    }
    
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }

        public bool? Signup { get; set; }
    }
}