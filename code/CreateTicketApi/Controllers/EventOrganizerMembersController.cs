using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using EventManagementDbAccess;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventOrganizerMembersController : ControllerBase
    {
        private readonly EventOrganizerMembersDbAccess _dbAccess;
        private readonly ILogger<EventOrganizerMembersController> _logger;

        public EventOrganizerMembersController(EventOrganizerMembersDbAccess dbAccess, ILogger<EventOrganizerMembersController> logger)
        {
            _dbAccess = dbAccess;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddMember([FromBody] EventOrganizerMembers member)
        {
            if (member == null)
                return BadRequest("Member cannot be null.");

            try
            {
                var id = await _dbAccess.AddMember(member);
                return Ok(new { OrganizerMemberId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member.");
                return StatusCode(500, "Error adding member.");
            }
        }

        [HttpGet("{organizerMemberId:int}")]
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

        [HttpGet("/byuserId/{userId:int}")]
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

        [HttpPut]
        public async Task<IActionResult> UpdateMember([FromBody] EventOrganizerMembers member)
        {
            if (member == null)
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

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteMember(int userId, [FromBody]int customerId)
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