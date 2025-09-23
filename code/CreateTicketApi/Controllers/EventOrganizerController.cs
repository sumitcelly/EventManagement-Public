using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventOrganizerController : ControllerBase
    {
        private readonly ILogger<EventOrganizerController> _logger;
        private readonly EventOrganizerDBAccess _organizerDbAccess;

        public EventOrganizerController(ILogger<EventOrganizerController> logger, EventOrganizerDBAccess organizerDbAccess)
        {
            _logger = logger;
            _organizerDbAccess = organizerDbAccess;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventOrganizer>> GetById(int id)
        {
            var organizer = await _organizerDbAccess.GetOrganizerById(id);
            if (organizer == null)
                return NotFound();

            //resetting fields which do not make sense when not authenticated.
            organizer.StripeAccountId = string.Empty;
          
            return organizer;
        }

        [HttpGet("ByName/{name}")]
        public async Task<ActionResult<EventOrganizer>> GetByName(string name)
        {
            var organizer = await _organizerDbAccess.GetOrganizerByName(name);
            if (organizer == null)
                return NotFound();
            return organizer;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Add([FromBody] EventOrganizer organizer)
        {
            if (organizer == null)
                return BadRequest("Invalid organizer.");
            var id = await _organizerDbAccess.AddOrganizer(organizer);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to add organizer.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EventOrganizer organizer)
        {
            if (organizer == null || id != organizer.OrganizerId)
                return BadRequest("Invalid organizer or ID mismatch.");
            var result = await _organizerDbAccess.UpdateOrganizer(organizer);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update organizer.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _organizerDbAccess.DeleteOrganizer(id);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete organizer.");
        }
    }
}