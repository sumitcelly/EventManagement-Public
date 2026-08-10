using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using EventUtils;
using Microsoft.AspNetCore.RateLimiting;
using GeoTimeZone;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("strict-ip-auth-organizer")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly EventDbAccess _EventDbAccess;

        private readonly TicketAccess _ticketContext;

        private readonly EmailCampaignDbAccess _emailCampaignDbAccess;

        private readonly NotificationTemplateAccess _notificationTemplateAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDbAccess;

        private readonly StripeAccess _stripeAccess;

        public EventsController(ILogger<EventsController> logger, EventDbAccess EventDbAccess,
                            TicketAccess ticketContext, 
                            EmailCampaignDbAccess emailCampaignDbAccess,
                             NotificationTemplateAccess notificationTemplateAccess,
                             StripeAccess stripeAccess, EventOrganizerDBAccess eventOrganizerDBAccess)
        {
            _logger = logger;
            _EventDbAccess = EventDbAccess;
            _ticketContext = ticketContext;
            _emailCampaignDbAccess = emailCampaignDbAccess;
            _notificationTemplateAccess = notificationTemplateAccess;
            _stripeAccess = stripeAccess;
            _eventOrganizerDbAccess = eventOrganizerDBAccess;
        }

        [EnableRateLimiting("public-browsing")]
        [HttpGet]
        [Route("Search")]
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

        [HttpGet("Basics/{eventId}")]
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

        [HttpGet("ByCustomer/{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<List<EventHeader>> GetEventsByCustomer(int customerId)
        {
            var evtList = await _EventDbAccess.GetEventListByCustomerId(customerId);
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(evtList));
            return evtList;
        }

        [HttpGet("ForScanning/{customerId}")]
        [Authorize(Policy = "ScanningAgent")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<List<EventHeader>> GetEventsForScanningByCustomer(int customerId)
        {
            var evtList = await _EventDbAccess.GetEventListForscanningByCustomerId(customerId);
            _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(evtList));
            return evtList;
        }

        [EnableRateLimiting("public-browsing")]
        [HttpGet("Details/{id}")]
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
        
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        [HttpGet("DetailsPreview/{customerId}/{customerName}/{eventName}")]
        public async Task<ActionResult<Event>> GetEventPreviewDetailsById(int customerId, string customerName, string eventName)
        {
            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(eventName))
            {
                return BadRequest("Invalid customer or event name"); 
            }
            try
            {
                var evt = await _EventDbAccess.GetEventDetailsByName(customerName, eventName);
                return evt;
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        
        [EnableRateLimiting("public-browsing")]
        [HttpGet("Details/{customerName}/{eventName}")]
        public async Task<ActionResult<Event>> GetEventDetailsByName(string customerName, string eventName)
        {
            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(eventName))
            {
                return BadRequest("Invalid customer or event name"); 
            }
            try
            {
                var evt = await _EventDbAccess.GetEventDetailsByName(customerName, eventName);
                return evt !=null && evt.IsLive?evt:NotFound();
            }
            catch (Exception)
            {
                return NotFound();
            }
           
        }



        [HttpGet("Settings/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<EventSettings>> GetEventSettings(int eventId)
        {
            if (eventId<=0)
            {
                return BadRequest("Invalid event."); 
            }
            var evt = await _EventDbAccess.GetEventSettings(eventId);
            if (evt == null)
                return NotFound();
            return evt;
        }

        [HttpPut("EventSettings/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<ActionResult<bool>> UpdateEventSettings(int eventId, [FromBody] EventSettings status)
        {
            if (eventId <= 0)
                return BadRequest("Invalid event.");
            if (!Enum.IsDefined(typeof(RefundMode), status.RefundMode) ||
                !Enum.IsDefined(typeof(TicketFeeMode), status.TicketFeeMode))
            {
                return BadRequest("Invalid data sent for event fee mode or refund mode");
            } 

            if (await _EventDbAccess.UpdateEventSettings(eventId, status))
            {
                if (!status.IsLive)
                {
                    // if event is being unpublished, also disable associated email campaigns
                    bool result = await _emailCampaignDbAccess.UpdateEmailCampaignsStatusForEvent(eventId,false);
                    _logger.LogInformation($"Event {eventId} unpublished. Associated email campaigns disabled: {result}");
                }
                else
                {
                    if (!await _emailCampaignDbAccess.CheckIfCamaignsExistForEvent(eventId))
                    {
                        _logger.LogInformation($"No campaigns exist for event id {eventId}. Going live for first time probably.");
                        List<EmailTemplate> templates=  await _notificationTemplateAccess.GetDefaultTemplates();
                        templates?.ForEach(async template =>
                        {
                            if (template.TemplateName ==  NotificationTemplateAccess.EventReminder2DayTemplateName ||
                                 template.TemplateName == NotificationTemplateAccess.EventReminder7DayTemplateName)
                            {
                                EmailCampaign campaign = new EmailCampaign
                                {
                                    Name = $"{template.TemplateName}_{status.EventUrlName}",
                                    Description = template.TemplateDescription,
                                    EventId = eventId,
                                    TemplateId = template.Id,
                                    Status = "Pending",
                                    SendAt = GetSendAtTime(template.TemplateName, status.EventDate),}
                                ;
                                if (await _emailCampaignDbAccess.CreateEmailCampaign(campaign) >0)
                                    _logger.LogInformation($"Created email campaign {campaign.Name} for event id {eventId} based on template {template.TemplateName}");
                                else
                                    _logger.LogError($"Failed to create email campaign {campaign.Name} for event id {eventId} based on template {template.TemplateName}");
                            }
                        });       
                    }
                    else
                    {
                        bool result = await _emailCampaignDbAccess.UpdateEmailCampaignsStatusForEvent(eventId,true);
                        _logger.LogInformation($"Campaigns already exist for event id {eventId}. Not creating default campaigns. But setting them to active. Result of status update is {result}");
                    }
                }
                return Ok(true);
            }
            else
            {
                return StatusCode(500, "Failed to update event settings.");
            }
        }

        private DateTime GetSendAtTime(string templateName, DateTime eventStartDate)
        {
            if (templateName == NotificationTemplateAccess.EventReminder7DayTemplateName)
            {
                return eventStartDate.AddDays(-7);
            }
            else if (templateName == NotificationTemplateAccess.EventReminder2DayTemplateName)
            {
                return eventStartDate.AddDays(-2);
            }
            else
            {
                throw new ArgumentException("Invalid template name for send time calculation");
            }
        }

        [HttpGet("GetIanaTimeZone/{lat}/{lng}")]
        public async Task<ActionResult<string>> GetIanaTimeZone(double lat, double lng)
        {
            var timezoneResult = TimeZoneLookup.GetTimeZone(lat, lng);
            return Ok(timezoneResult.Result);
        }

        [HttpPost("{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<ActionResult<int>> CreateEvent(int customerId, [FromBody] Event evt)
        {
            if (evt == null || customerId <= 0 || customerId != evt.EventOrganizerId)
                return BadRequest("Invalid event.");

            if (string.IsNullOrWhiteSpace(evt.StreetAddress) || string.IsNullOrWhiteSpace(evt.City) ||
             string.IsNullOrWhiteSpace(evt.State) || string.IsNullOrWhiteSpace(evt.ZipCode))
            {
                return BadRequest("event address details are not set");
            }
            EventOrganizer organizer= await _eventOrganizerDbAccess.GetOrganizerById(customerId);
            if (organizer == null)
            {
                _logger.LogError($"Unable to locate event orgnaizer details for {customerId}");
                return StatusCode(500, "Unable to locate event organizer details");
            }
             
            if (!string.IsNullOrWhiteSpace(organizer.StripeAccountId))
            {
                evt.LocationId = await _stripeAccess.GetLocationIdForAddress(evt.EventName, evt.StreetAddress, evt.City,evt.State, evt.ZipCode, organizer.StripeAccountId);
            }
            else
            {
                evt.LocationId = string.Empty;
                _logger.LogInformation($"Stripe account not setup {customerId}. Not creating locationId.");      
            }
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
            Console.WriteLine($"event id {eventId} and event {evt.Category} received for update");
            if (evt == null || eventId != evt.EventId)
                return BadRequest("Invalid event or ID mismatch.");
            if (string.IsNullOrWhiteSpace(evt.StreetAddress) || string.IsNullOrWhiteSpace(evt.City) ||
             string.IsNullOrWhiteSpace(evt.State) || string.IsNullOrWhiteSpace(evt.ZipCode))
            {
                return BadRequest("event address details are not set");
            }
            //this should be ok since the event should be cached
            Event evtCurrent = await _EventDbAccess.GetEventDetailsById(eventId);
            if (evtCurrent == null)
            {
                return  StatusCode(500,"the event could not be located");
            }

            EventOrganizer organizer= await _eventOrganizerDbAccess.GetOrganizerById(evt.EventOrganizerId);
            if (organizer == null)
            {
                _logger.LogError($"Unable to locate event orgnaizer details for {evt.EventOrganizerId}");
                return StatusCode(500, "Unable to locate event organizer details");
            }
            //always creating location id to avoid caching bugs.
            if (!string.IsNullOrWhiteSpace(organizer.StripeAccountId))
            {
                evt.LocationId = await _stripeAccess.GetLocationIdForAddress(evt.EventName, evt.StreetAddress, evt.City,evt.State, 
                                            evt.ZipCode, organizer.StripeAccountId,evt.Country);
            }
               
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
