using Amazon.S3.Model;
using EventManagementDbAccess;
using EventUtils;
using K4os.Compression.LZ4.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        private readonly EmailTransactionLogDbAccess _emailTransactionLogAccess;
        private readonly IConfiguration _configuration;
        public UserController(ILogger<UserController> logger, UserDbAccess userDbAccess,
                EventOrganizerMembersDbAccess eventOrganizerMembersDbAccess, JwtUtils tokenUtils,
                LoginCodesDbAccess loginCodesDbAccess,
                SQSHelper sqsHelper,
                NotificationTemplateAccess templateDbAccess,
                EmailTransactionLogDbAccess emailTransactionLogAccess,
                IConfiguration config)
        {
            _logger = logger;
            _userDbAccess = userDbAccess;
            _eventOrganizerMembersDbAccess = eventOrganizerMembersDbAccess;
            _tokenUtils = tokenUtils;
            _loginCodesDbAccess = loginCodesDbAccess;
            _sqsHelper = sqsHelper;
            _templateDbAccess = templateDbAccess;
            _emailTransactionLogAccess = emailTransactionLogAccess;
            _configuration = config;
        }

        /// <summary>
        /// Not used so far. mark obsolete later.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("{email}")]
        public async Task<ActionResult<EventUser>> GetUserByEmail(string email)
        {
            return BadRequest();
            // if (string.IsNullOrEmpty(email))
            //     return BadRequest("Email is null or empty.");
            // var user = await _userDbAccess.GetUserByEmail(email);
            // if (user == null)
            //     return NotFound();
            // return user;
        }

        /// <summary>
        /// Main login method that validates user credentials and returns a JWT token.  
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [EnableRateLimiting("strict-ip-auth")]
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
            string role = string.Empty;
            if (orgMember!=null && orgMember.IsActive)
            {
                role = orgMember.Role;
            }
            else
            {
                role = UserRoles.Attendee.ToString(); // Default to "User" if no organization member found or not active
            }
            _logger.LogInformation($"User {email} logged in with role {role}.");


            //create a JWT token or session here as needed
            var _accessToken = _tokenUtils.GenerateJwtToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0, user.Name ?? string.Empty);
            string refreshToken = await _tokenUtils.GenerateRefreshToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0,user.Name ?? string.Empty);
           // Response.Cookies.Append("refreshToken", refreshToken);
            SetSecureCookie("refreshToken", refreshToken);

            return Ok(new
            {
                accessToken = _accessToken,
                user = new { id = user.UserId, email = request.Email, guest=false,role = role ?? null, customerId = orgMember?.CustomerId ?? 0, 
                            name = user.Name ?? string.Empty }
            });
        }

    
        [HttpPost("refresh")]
        //Keep an eye on thos rate limit
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
          
            if (string.IsNullOrEmpty(refreshToken) )
                return Unauthorized();


            if (!await _tokenUtils.ValidateJwtToken(refreshToken))
                return Unauthorized("Invalid refresh token.");

            //var userId = RefreshTokens[refreshToken];
            var claims = _tokenUtils.GetClaimsFromToken(refreshToken);
            string userId = claims.Item1;
            string role = claims.Item2;
            string customerId = claims.Item3;
            string userName = claims.Item4;        
            await _tokenUtils.RevokeTokenInCache(refreshToken); // Revoke the old refresh token in cache
            //when stored in db, no need to store userId, role, customerId in token
            var newRefreshToken = await _tokenUtils.GenerateRefreshToken(userId.ToString(), role, Convert.ToInt16(customerId),userName);
            
            SetSecureCookie("refreshToken", newRefreshToken);

            var newAccessToken = _tokenUtils.GenerateJwtToken(userId, role, Convert.ToInt16(customerId), userName);

            return Ok(new { accessToken = newAccessToken });
        }
        private void SetSecureCookie(string name, string value)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, 
                //for cross site mobile access, need to set SameSiteMode.None and set Secure=true in production
                SameSite = SameSiteMode.None,
                Path = "/"
            };
            Response.Cookies.Append(name, value, options);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            return BadRequest();
            // var refreshToken = Request.Cookies["refreshToken"];
            // if (string.IsNullOrEmpty(refreshToken))
            //     return Unauthorized();
        
            // if (!await _tokenUtils.ValidateJwtToken(refreshToken))
            //     return Unauthorized("Invalid refresh token.");
        
            // var claims = _tokenUtils.GetClaimsFromToken(refreshToken);
            // string userId = claims.Item1;
            // string role = claims.Item2;
            // string customerId = claims.Item3;
        
            // var user = await _userDbAccess.GetUserById(int.Parse(userId));
            // if (user == null)
            //     return NotFound("User not found.");
        
            // return Ok(new
            // {
            //     id = user.UserId,
            //     email = user.Email,
            //     role = role,
            //     customerId = customerId,
            //     name = user.Name,
            //     guest=false
            // });
        }
        
        /// <summary>
        /// not being used so far. But need to send in the original password as well for verification.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ResetPassword")]
        [EnableRateLimiting("strict-ip-auth")]
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

        /// <summary>
        /// This method is for verifying the email code sent to user for login or signup.  If code is valid, it will return a JWT token for authentication.  For signup, if user does not exist, it will create a new user record.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("VerifyEmailCode")]
        [EnableRateLimiting("strict-ip-auth")]
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
            var _accessToken = _tokenUtils.GenerateJwtToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0, user.Name ?? string.Empty);
            string refreshToken = await _tokenUtils.GenerateRefreshToken(user.UserId.ToString(), role, orgMember?.CustomerId ?? 0, user.Name ?? string.Empty);
           // Response.Cookies.Append("refreshToken", refreshToken);
            SetSecureCookie("refreshToken", refreshToken);

            return Ok(new
            {
                accessToken = _accessToken,
                user = new { id = user.UserId, name= user.Name, guest=false, email = request.Email, role = role ?? null, customerId = orgMember?.CustomerId ?? 0 }
            });
        }

        [HttpPost("setnewmemberpassword")]
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<IActionResult> SetNewMemberPassword([FromBody] SetNewMemberPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.InvitationToken) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Token or new password is null or empty.");
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.UserName))
                return BadRequest("Email or UserName is null or empty.");

            var member = await _eventOrganizerMembersDbAccess.GetMemberByInvitationToken(request.InvitationToken);  
            if (member == null)
                return NotFound("Member not found.");           
            if (member.IsActive || DateTime.UtcNow > member.CreatedAt.AddDays(3)) // Assuming token expires after 3 days
                return BadRequest("Unable to set password. Invitation token is either already used or expired.");

            var result = await  _userDbAccess.ResetPassword(member.UserId, request.Password);
            _logger.LogInformation($"Set new password for user {member.UserId} with result {result}");
            if (result)
            {
                if (await _eventOrganizerMembersDbAccess.SetMemberToActive(member.OrganizerMemberId,member.CustomerId)) // Activate the member after setting password 
                {
                    _logger.LogInformation("Activated member {memberId} for user {userId} in org {customerId}", member.OrganizerMemberId, member.UserId, member.CustomerId);
                
                    //create a JWT token or session here as needed
                    var _accessToken = _tokenUtils.GenerateJwtToken(member.UserId.ToString(), member.Role, member.CustomerId, request.UserName ?? string.Empty);
                    string refreshToken = await _tokenUtils.GenerateRefreshToken(member.UserId.ToString(), member.Role, member.CustomerId, request.UserName ?? string.Empty);
                    SetSecureCookie("refreshToken", refreshToken);
                    return Ok(new
                    {
                        accessToken = _accessToken,
                        user = new { id = member.UserId, name= request.UserName, guest=false, email = request.Email, role = member.Role ?? null, customerId = member.CustomerId }
                    });
                }
                else
                {
                    _logger.LogError("Failed to activate member {memberId} for user {userId} in org {customerId} after setting password", member.OrganizerMemberId, member.UserId, member.CustomerId);
                    return StatusCode(500, "Failed to activate member after setting password.");
                }
            }
            return StatusCode(500, "Failed to set new password.");
         }

        // /// <summary>
        // ///This call can lead to enumeration of email addresses in DB. Do not expose it.
        //  Called during signup to check if user already exists.  If exists, return true.
        // /// If exists, the true is passed in the next call to GenerateEmailCode to continue login flow.
        // /// </summary>
        // /// <param name="email"></param>
        // /// <returns></returns>
        // [HttpGet("CheckUserExists/{email}")]
        // public async Task<bool> CheckUserExists(string email)
        // {
        //     var user =  await _userDbAccess.GetUserByEmail(email);
        //     return user != null;
        // }

        /// <summary>
        /// Generates a one-time email code for login or signup and sends it to the user's email address.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="signup"></param>
        /// <returns></returns>
        [HttpGet("GenerateEmailCode/{email}/{signup?}")]
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<ActionResult<string>> GenerateEmailCode(string email, bool? signup = false)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is null or empty.");
            var user = await _userDbAccess.GetUserByEmail(email);
            if (user == null && signup != true)
            {
                _logger.LogError($"User with email  {email} was not found.");
                return Ok();
            }
            
            if (user == null && signup == true)
            {
                _logger.LogInformation($"User with email {email} not found, continuing since this is signup.");
               //temp user is created during VerifyEmailCode step not here.
            }
            bool signupExists = false;
            if (user != null && signup == true)
            {
                _logger.LogInformation($"User {email} already exists and tryig to singup, sending them one time code.");
                signupExists = true;
            }

            string emailCode= EventUtils.PasswordGenerator.GetPassword();
            var code = await _loginCodesDbAccess.CreateLoginCode(new LoginCode
            {
                EmailAddress = email,
                SecurityCode =emailCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                RequestIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            if (code <= 0)
                return StatusCode(500, "Failed to generate email verification code.");
            
            Tuple<string,string> templateData = await _templateDbAccess.GetDefaultTemplateDetailsByName(NotificationTemplateAccess.EmailVerificationTemplateName);
            if (templateData == null || string.IsNullOrEmpty(templateData.Item1))
                return StatusCode(500, "Email template not found.");

            EmailTokenReplacement tokenReplacer = new EmailTokenReplacement(_configuration);
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                EmailCode = emailCode,
            });
            
            int id = await _emailTransactionLogAccess.InsertEmailTransactionLog(new EmailTransactionLog
            {
                RecipientEmail = email,
                EmailType = "EmailVerification",
                Status = "Queued",
                CreatedAt = DateTime.UtcNow,
                RefId = user?.UserId ?? 0,
            });

            string content = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(templateData.Item1)), values);
          
            QueueResponse resp =  await _sqsHelper.QueueMessage(email,
                user?.Name ?? "User",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                tokenReplacer.ReplacePlatformNameInSubject(templateData.Item2),
                id
                );
            
            if (!resp.Success)
            {
                string status = resp.Retry ? "QueuingFailure_Retry" : "QueuingFailure_NoRetry";
                await _emailTransactionLogAccess.UpdateEmailTransactionLogStatus(id, status, resp.Message);
            }

            return Ok(new {signupExists});
        }

        [HttpPost("logout")]
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                //RefreshTokens.Remove(refreshToken);
                await _tokenUtils.RevokeTokenInCache(refreshToken); // Revoke the refresh token in cache
                Response.Cookies.Delete("refreshToken");
            }

            return Ok(new { message = "Logged out" });
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] EventUser user)
        {
            return NotFound();
            // if (user == null)
            //     return BadRequest("Invalid user.");
            // var id = await _userDbAccess.CreateUser(user);
            // if (id > 0)
            //     return Ok(id);
            // return StatusCode(500, "Failed to create user.");
        }

        /// <summary>
        /// After the user verifies their email code as part of login or signup, this method called to set their password and maybe other data in the future.
        ///   For now, it only updates password but can be extended to update other user info as well.  It requires the email in the URL to match the email in the body for security check, and also requires authentication to make sure only logged in user can update their info.
        ///  /// </summary>
        /// <param name="email"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// 
        [Authorize]
        [HttpPut("{email}")]
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<IActionResult> Update(string email, [FromBody] EventUser user)
        {
            //verify if user id in claim matches userid in the body, and email in the body matches email in the URL for security check
            if (user == null || string.IsNullOrEmpty(email) || user.Email != email)
                return BadRequest("Invalid user or email mismatch.");
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || userId != user.UserId.ToString())
                return Unauthorized("User ID mismatch.");

            var result = await _userDbAccess.UpdateUserByEmail(user);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update user.");
        }

       
   
        [Authorize]
        [HttpPost("UserSignup")]
         [EnableRateLimiting("strict-ip-auth")]
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

    public class SetNewMemberPasswordRequest
    {
        public required string InvitationToken { get; set; }
        public required string Password { get; set; }

        public required string UserName { get; set; }

        public required string Email { get; set;}
    }
}