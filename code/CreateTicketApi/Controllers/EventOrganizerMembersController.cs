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

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventOrganizerMembersController : ControllerBase
    {
        private readonly EventOrganizerMembersDbAccess _dbAccess;
        private readonly ILogger<EventOrganizerMembersController> _logger;
        private readonly UserDbAccess _userdbAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDbAcces;

         private readonly JwtUtils _tokenUtils;
        public EventOrganizerMembersController(EventOrganizerMembersDbAccess dbAccess, UserDbAccess userDbAccess,
                        EventOrganizerDBAccess eventOrganizerDbAcces, JwtUtils jwtUtils,
                     ILogger<EventOrganizerMembersController> logger)
        {
            _dbAccess = dbAccess;
            _logger = logger;
            _userdbAccess = userDbAccess;
            _eventOrganizerDbAcces = eventOrganizerDbAcces;
            _tokenUtils = jwtUtils;
        }

        /// <summary>
        /// Not used so far
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("AddOwner/{userId}")]
        [Authorize(Policy = "MatchingUserId")]
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
            int memberId = await _dbAccess.AddMember(member);
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
                        member.UserId = user.UserId;
                    }
                }

                if (member.UserId > 0)
                {
                    var id = await _dbAccess.AddMember(member);
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

        [HttpGet("{organizerMemberId:int}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        public async Task<IActionResult> GetMemberById(int organizerMemberId)
        {
            try
            {
                var member = await _dbAccess.GetMemberById(organizerMemberId);
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