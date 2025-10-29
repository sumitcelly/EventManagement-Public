
using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        [Route("/eventitemtype/all/{eventId}")]
        public async Task<ActionResult<List<EventItemType>>> GetAll(int eventId)
        {
            if (eventId <= 0)
                return BadRequest("Invalid event ID.");
            var items = await _eventItemTypeDbAccess.GetAllEventItemTypesByEventId(eventId);
            _logger.LogInformation("EventItemTypes received: {0}", items.Count);
            return items;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventItemType>> GetById(int id)
        {
            var item = await _eventItemTypeDbAccess.GetEventItemTypeById(id);
            if (item == null)
                return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] EventItemType item)
        {
            if (item == null)
                return BadRequest("Invalid item.");
            var id = await _eventItemTypeDbAccess.CreateEventItemType(item);
            if (id > 0)
                return Ok(id);
            return StatusCode(500, "Failed to create EventItemType.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EventItemType item)
        {
            if (item == null || id != item.EventItemTypeId)
                return BadRequest("Invalid item or ID mismatch.");
            var result = await _eventItemTypeDbAccess.UpdateEventItemType(item);
            if (result)
                return Ok();
            return StatusCode(500, "Failed to update EventItemType.");
        }

        [HttpDelete("{id}")]
        public async Task<string> Delete(int id)
        {
            return  await _eventItemTypeDbAccess.DeleteEventItemType(id);
           
        }
    }
}