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


        public EventsController(ILogger<EventsController> logger, EventDbAccess EventDbAccess,
                            TicketAccess ticketContext)
        {
            _logger = logger;
            _EventDbAccess = EventDbAccess;
            _ticketContext = ticketContext;
  
        }

        [HttpGet]
        [Route("/Events/Search")]
        public async Task<List<EventHeader>> SearchEvents(string keyword = null,
                                                         DateOnly startDate = default,
                                                         int intervalDay = 0,
                                                         string city = null,
                                                         string state = null,
                                                         string category = null,
                                                         int limit = 10, DateTime cursor =default(DateTime))
        {
            if (startDate == default)
                startDate = DateOnly.MinValue;
            Console.WriteLine("SearchEvents called with keyword:{0}, startDate:{1}, intervalDay:{2}, city:{3}, state:{4}, category:{5}, limit:{6}, offset:{7}",
                                keyword, startDate, intervalDay, city, state, category, limit, cursor);
            List<EventHeader> events = await _EventDbAccess.SearchEvents(keyword, startDate, intervalDay, city, state, category, limit, cursor);
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(events));
            return events;
        }

        [HttpGet("/Events/Basics/{id}")]
        public async Task<ActionResult<EventHeader>> GetEventBasicsById(int id)
        {
            var evt = await _EventDbAccess.GetEventHeaderById(id);
            if (evt == null)
                return NotFound();
            return evt;
        }

        [HttpGet("/Events/Details/{id}")]
        public async Task<ActionResult<Event>> GetEventDetailsById(int id)
        {
            var evt = await _EventDbAccess.GetEventDetailsById(id);
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
            Console.WriteLine($"event id {id} and event {evt} received for update");
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
