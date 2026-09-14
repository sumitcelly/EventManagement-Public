using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using EventManagementDbAccess;
using K4os.Compression.LZ4.Internal;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using EventUtils;
using System.Security.Claims;
using CreateTicketApi.BusinessLogic;
using Microsoft.AspNetCore.RateLimiting;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("strict-ip-auth-organizer")]
    public class EventOrganizerMembersController : ControllerBase
    {
        private readonly EventOrganizerMembersDbAccess _dbAccess;
        private readonly ILogger<EventOrganizerMembersController> _logger;
        private readonly UserDbAccess _userdbAccess;
        private readonly EmailUtils _emailUtils;

        private readonly EventOrganizerDBAccess _eventOrganizerDbAcces;

         private readonly JwtUtils _tokenUtils;
        public EventOrganizerMembersController(EventOrganizerMembersDbAccess dbAccess, UserDbAccess userDbAccess,
                        EventOrganizerDBAccess eventOrganizerDbAcces, JwtUtils jwtUtils,
                     ILogger<EventOrganizerMembersController> logger,
                     EmailUtils emailUtils)
        {
            _dbAccess = dbAccess;
            _logger = logger;
            _userdbAccess = userDbAccess;
            _eventOrganizerDbAcces = eventOrganizerDbAcces;
            _tokenUtils = jwtUtils;
            _emailUtils = emailUtils;
        }

        /// <summary>
        /// Not used so far
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("AddOwner/{userId}")]
        [Authorize(Policy = "MatchingUserId")]
        [EnableRateLimiting("strict-ip-auth")]
        public async Task<IActionResult> AddOwner(int userId,EventOrganizerMembers data)
        {
            if (userId <= 0 || data == null || data.CustomerId <= 0)
            {
                return BadRequest("Invalid data sent");
            }

            int customerId = data.CustomerId;
            string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (role != UserRoles.Attendee.ToString())
            {
                return BadRequest($"Logged in role is incorrect in order to become an organizer: {role}");
            }
            
            EventUser user = await _userdbAccess.GetUserById(userId);
            if (user == null)
            {
                return NotFound("Unable to locate user sent");
            }

           
            List<EventOrganizerMembers> members = await _dbAccess.GetMembersByCustomerId(customerId);
            if (members?.Count > 0)
            {
                return Forbid("There can only be one owner for an organization");
            }

            DateTime createdDate = await _eventOrganizerDbAcces.GetOrganizerCreatedDate(customerId);
            if ((DateTime.Now - createdDate).TotalSeconds > 20)
            {
                return Forbid("The organizer cannot be added as owner due to a security issue.");
            }
            EventOrganizerMembers member = new EventOrganizerMembers()
            {
                UserId = userId,
                CustomerId = customerId,
                IsActive= true,
                Role = UserRoles.Owner.ToString()
            };
            (int memberId,string token) = await _dbAccess.AddMember(member);
            if (memberId >0)
            {
                _logger.LogInformation($"Added user {userId} as owner of org {customerId}");
                var newRefreshToken = _tokenUtils.GenerateRefreshToken(userId.ToString(), UserRoles.Owner.ToString(), customerId);      
                //SetSecureCookie("refreshToken", newRefreshToken);
                var newAccessToken = _tokenUtils.GenerateJwtToken(userId.ToString(), UserRoles.Owner.ToString(), customerId);
                return Ok(new { accessToken = newAccessToken,
                                user = new { id = userId, email = user.Email, role = UserRoles.Owner.ToString(), customerId = customerId, 
                                name = user.Name ?? string.Empty } });
            }
            else
            {
                return  StatusCode(500,"Error adding member as owner");
            }
            
        }
      
        [HttpPost("ResendInvitation/{customerId}/{memberId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> ResendInvitation(int customerId, int memberId)
        {
            if (customerId <= 0 || memberId <= 0)
                return BadRequest("Invalid customer or member ID.");

            string result = await _dbAccess.GenerateNewInvitationToken(customerId, memberId);
            if (!string.IsNullOrEmpty(result))
            {
                EventOrganizerMembers? member = await _dbAccess.GetMemberById(memberId, customerId);
                if (member == null)
                    return NotFound("Member not found.");
                member.InvitationToken = result;
                string inviterName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "A team member";
                await _emailUtils.SendMemberInvitationEmail(customerId,inviterName, member);
    
                return Ok("Invitation email sent successfully.");
            }
            else
            {
                return StatusCode(500, "Error regenerating invitation token.");
            }
        }

        [HttpPost("{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> AddMember(int customerId, [FromBody] EventOrganizerMembers member)
        {
            if (member == null || customerId <= 0 || member.CustomerId != customerId)
                return BadRequest("Member cannot be null.");

            try
            {
                EventUser? user = null;
                if (member.UserId == 0 && !string.IsNullOrWhiteSpace(member.Email))
                {
                    //A user can be linked to multiple  customers. 
                    //TODO: Will need to enhance logic at login when we reach that stage to prompt user for which customer/organization they want to login to if they are linked to multiple ones. For now we will just link to the first one we find which is not ideal but should work for testing purposes.
                    user = await _userdbAccess.GetUserByEmail(member.Email);
                    if (user == null)
                    {

                        int userId = await _userdbAccess.CreateUser(
                            user = new EventUser()
                            {
                                Email = member.Email,
                                Name = member.FullName,

                            }
                        );
                        if (userId > 0)
                        {
                            _logger.LogInformation($"Created user id {userId} for adding to organization {member.CustomerId} ");
                            member.UserId = userId;
                            
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Retrived user id {user.UserId} for adding to organization {member.CustomerId} ");
                        EventOrganizerMembers? memberList= await _dbAccess.GetContainingOrgByUserId(user.UserId);
                        if (memberList!=null)
                        {
                            _logger.LogInformation($"The user with email {member.Email} already is connected to org with id {memberList.CustomerId}");
                            return Conflict("Member with this email already connected to another organizer. Please use another email address.");
                        }
                        member.UserId = user.UserId;
                     
                    }
                }

                if (member.UserId > 0)
                {
                    (int id, string token)= await _dbAccess.AddMember(member);
                    member.InvitationToken = token;
                   
                    string inviterName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "A team member";
                    await _emailUtils.SendMemberInvitationEmail(customerId,inviterName, member);
                    return Ok(new { OrganizerMemberId = id });
                }
                else
                {
                    return StatusCode(500, "Unable to retrive or add user to add member to organization");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member.");
                return StatusCode(500, "Error adding member.");
            }
        }

        [EnableRateLimiting("strict-ip-auth")]
        [HttpGet("validateToken/{token}")]
        public async Task<IActionResult> ValidateInvitationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token must be provided.");

            try
            {
                var member = await _dbAccess.GetMemberByInvitationToken(token);
                if (member == null)
                    return NotFound("Invalid token.");
                if (member.IsActive || DateTime.UtcNow > member.CreatedAt.AddDays(3)) // Assuming token expires after 3 days
                    return BadRequest("Token has already been used or has expired.");
                return Ok(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token.");
                return StatusCode(500, "Error validating invitation token.");
            }
        }

        [HttpGet("{organizerMemberId:int}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        public async Task<IActionResult> GetMemberById(int organizerMemberId)
        {
            try
            {
                string custId = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value ?? "0";
                if (custId == "0" || !int.TryParse(custId, out int id))
                    return Forbid("CustomerId claim is missing in token.");
                var member = await _dbAccess.GetMemberById(organizerMemberId, id);
                if (member == null)
                    return NotFound();
                return Ok(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member.");
                return StatusCode(500, "Error retrieving member.");
            }
        }


        [HttpGet("bycustomer/{customerId:int}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> GetMembersByCustomerId(int customerId)
        {
            try
            {
                var members = await _dbAccess.GetMembersByCustomerId(customerId);
                return Ok(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving members.");
                return StatusCode(500, "Error retrieving members.");
            }
        }

        [HttpGet("byuserId/{userId:int}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingUserId")]
        public async Task<IActionResult> GetMemberByUserId(int userId)
        {
            try
            {
                var member = await _dbAccess.GetContainingOrgByUserId(userId);
                if (member == null)
                    return NotFound();
                return Ok(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member.");
                return StatusCode(500, "Error retrieving member.");
            }
        }

        [HttpPut("{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> UpdateMember(int customerId,[FromBody] EventOrganizerMembers member)
        {
            if (member == null || customerId == 0 || member.CustomerId != customerId)
                return StatusCode(500, "Bad input data");

            try
            {
                
                var result = await _dbAccess.UpdateMember(member);
                if (!result)
                    return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member.");
                return StatusCode(500, "Error updating member.");
            }
        }

        [HttpDelete("{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> DeleteMember(int customerId,[FromBody] int userId)
        {
            try
            {
                if (userId < 0 || customerId < 0)
                    return StatusCode(500, "Invalid data sent for deletion");
                var result = await _dbAccess.DeleteMember(userId,customerId);
                if (!result)
                    return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member.");
                return StatusCode(500, "Error deleting member.");
            }
        }
    }
}