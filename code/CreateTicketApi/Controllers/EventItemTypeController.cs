
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventItemTypeController : ControllerBase
    {
        private readonly ILogger<EventItemTypeController> _logger;
        private readonly EventItemTypeDbAccess _eventItemTypeDbAccess;

        public EventItemTypeController(ILogger<EventItemTypeController> logger, EventItemTypeDbAccess eventItemTypeDbAccess)
        {
            _logger = logger;
            _eventItemTypeDbAccess = eventItemTypeDbAccess;
        }

        //This cannot be authorized since it is used by the public API to get the item types for an event. We will need to validate the event id and only return item types for valid events.
        
        [EnableRateLimiting("public-browsing")]
        [HttpGet]
        [Route("all/{eventId}")]
        public async Task<ActionResult<List<EventItemType>>> GetAll(int eventId)
        {
            if (eventId <= 0)
                return BadRequest("Invalid event ID.");
            var items = await _eventItemTypeDbAccess.GetAllEventItemTypesByEventId(eventId);
            _logger.LogInformation("EventItemTypes received: {0}", items.Count);
            return items;
        }

        [EnableRateLimiting("strict-ip-auth-organizer")]
        [HttpGet("{eventId}/{id}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<EventItemType>> GetById(int eventId, int id)
        {
            if (eventId <= 0 || id <= 0)
                return BadRequest("Invalid customer ID or item ID.");
            var item = await _eventItemTypeDbAccess.GetEventItemTypeById(id);
            if (item == null)
                return NotFound();
            return item;
        }

        [EnableRateLimiting("strict-ip-auth-organizer")]
        [HttpPost("{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<int>> Create(int eventId, [FromBody] EventItemType item)
        {
            if (item == null || eventId <= 0 || item.EventId != eventId)
                return BadRequest("Invalid item.");
            var id = await _eventItemTypeDbAccess.CreateEventItemType(item);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create EventItemType.");
        }

        [EnableRateLimiting("strict-ip-auth-organizer")]
        [HttpPut("{eventId}/{id}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<IActionResult> Update(int eventId,int  id, [FromBody] EventItemType item)
        {
            if (item == null || id != item.EventItemTypeId || eventId != item.EventId)
                return BadRequest("Invalid item or ID mismatch.");
            var result = await _eventItemTypeDbAccess.UpdateEventItemType(item);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update EventItemType.");
        }

        [EnableRateLimiting("strict-ip-auth-organizer")]
        [HttpDelete("{eventId}/{id}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<IActionResult> Delete(int eventId, int id)
        {
            if ( eventId <= 0 || id <= 0)
                return BadRequest("Invalid event ID or item ID.");
            var result = await _eventItemTypeDbAccess.DeleteEventItemType(eventId,id);
            if (string.IsNullOrEmpty(result))
                return Ok();
            return StatusCode(500, "Failed to delete EventItemType with error message: " + result);
        
        }
    }
}