using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<List<EventHeader>> SearchEvents(string keyword = null ,
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

        [HttpGet("/Events/Basics/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        public async Task<ActionResult<EventHeader>> GetEventBasicsById(int eventId)
        {
            var evt = await _EventDbAccess.GetEventHeaderById(eventId);
            if (evt == null)
                return NotFound();
            if (evt.EventOrganizerId != int.Parse(User.FindFirst("CustomerId")?.Value ?? "0"))
            {
                return Forbid();
            }
            return evt;
           
        }

        [HttpGet("/Events/ByCustomer/{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<List<EventHeader>> GetEventsByCustomer(int customerId)
        {
            var evtList = await _EventDbAccess.GetEventListByCustomerId(customerId);
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(evtList));
            return evtList;
        }

        [HttpGet("/Events/ForScanning/{customerId}")]
        [Authorize(Policy = "ScanningAgent")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<List<EventHeader>> GetEventsForScanningByCustomer(int customerId)
        {
            var evtList = await _EventDbAccess.GetEventListForscanningByCustomerId(customerId);
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(evtList));
            return evtList;
        }

        [HttpGet("/Events/Details/{id}")]
        public async Task<ActionResult<Event>> GetEventDetailsById(int id)
        {
            if (id<=0)
            {
                return BadRequest("Invalid event."); 
            }
            var evt = await _EventDbAccess.GetEventDetailsById(id);
            if (evt == null)
                return NotFound();
            return evt;
        }


        [HttpGet("/Events/Details/{customerName}/{eventName}")]
        public async Task<ActionResult<Event>> GetEventDetailsByName(string customerName, string eventName)
        {
            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(eventName))
            {
                return BadRequest("Invalid customer or event name"); 
            }
            var evt = await _EventDbAccess.GetEventDetailsByName(customerName, eventName);
            if (evt == null)
                return NotFound();
            return evt;
        }



        [HttpGet("/Events/LiveStatus/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<EventLiveStatus>> GetEventLiveStatusById(int eventId)
        {
            if (eventId<=0)
            {
                return BadRequest("Invalid event."); 
            }
            var evt = await _EventDbAccess.GetLiveStatusForEvent(eventId);
            if (evt == null)
                return NotFound();
            return evt;
        }

        [HttpPut("/Events/LiveStatus/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<bool>> UpdateLiveStatus(int eventId, [FromBody] bool status)
        {
            if (eventId <= 0)
                return BadRequest("Invalid event.");
            return (await _EventDbAccess.UpdatePublishStatus(eventId, status))?true:false;
        }

        [HttpPost("/events/{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<ActionResult<int>> CreateEvent(int customerId, [FromBody] Event evt)
        {
            if (evt == null || customerId <= 0 || customerId != evt.EventOrganizerId)
                return BadRequest("Invalid event.");
   
            var id = await _EventDbAccess.CreateEvent(evt);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create event.");
        }

        [HttpPut("{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<IActionResult> UpdateEvent(int eventId, [FromBody] Event evt)
        {
            Console.WriteLine($"event id {eventId} and event {evt} received for update");
            if (evt == null || eventId != evt.EventId)
                return BadRequest("Invalid event or ID mismatch.");
            bool result = await _EventDbAccess.UpdateEvent(evt);
            if (result)
                return Ok();

            return StatusCode(500, "Failed to update event.");
        }

        [HttpDelete("{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var result = await _EventDbAccess.DeleteEvent(eventId);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to delete event.");
        }
    }
}
