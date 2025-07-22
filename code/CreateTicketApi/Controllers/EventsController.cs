using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly EventDbAccess _EventDbAccess;

        private readonly TicketAccess _ticketContext;
        public EventsController(ILogger<EventsController> logger, EventDbAccess EventDbAccess, TicketAccess ticketContext)
        {
            _logger = logger;
            _EventDbAccess = EventDbAccess;
            _ticketContext = ticketContext;
        }

        [HttpGet]
        [Route("/Events/All")]
        public async Task<List<Event>> GetEvents()
        {
            List<Event> events = await _EventDbAccess.GetAllEvents();
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(events));
            return events;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            var evt = await _EventDbAccess.GetEventById(id);
            if (evt == null)
                return NotFound();
            return evt;
        }

        [HttpGet("ByName/{name}")]
        public async Task<ActionResult<Event>> GetEventByName(string name)
        {
            var evt = await _EventDbAccess.GetEventByName(name);
            if (evt == null)
                return NotFound();
            return evt;
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateEvent([FromBody] Event evt)
        {
            if (evt == null)
                return BadRequest("Invalid event.");
            var id = await _EventDbAccess.CreateEvent(evt);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create event.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event evt)
        {
            if (evt == null || id != evt.EventId)
                return BadRequest("Invalid event or ID mismatch.");
            var result = await _EventDbAccess.UpdateEvent(evt);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update event.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _EventDbAccess.DeleteEvent(id);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete event.");
        }
    }
}
