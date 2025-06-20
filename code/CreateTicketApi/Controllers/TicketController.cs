using EventDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace CreateTicketApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TicketController : ControllerBase
{
    private readonly ILogger<TicketController> _logger;

    private readonly TicketAccess _ticketContext;
    private readonly NotificationTemplateAccess _templateAccess;
    private readonly EventOrganizerDBAccess _eventOrganizerDBAccess;
    private EventContext _event;
    private readonly SQSHelper _sqsClient  =  null;
    public TicketController(IConfiguration config, ILogger<TicketController> logger,
                            TicketAccess ticketContext, NotificationTemplateAccess templateAccess,
                            EventOrganizerDBAccess eventOrganizerDBAccess,
                            EventContext eventObject)
    {
        _logger = logger;
        _ticketContext = ticketContext;
        _templateAccess = templateAccess;
        _eventOrganizerDBAccess = eventOrganizerDBAccess;
        _event =  eventObject;
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }
        _sqsClient = new SQSHelper(config);
    }


    [HttpPost("GetTicketCodeByQR")]
    public string GetTicketCodeByQR(byte[] qrCode)
    {
        return QRCodeUtils.GetQRText(qrCode);
    }


    [HttpGet]
    public async Task<EventSalesItem> GetTicketByQRCode(string qrCode, int eventId=1)
    {
        return await _ticketContext.GetEventTicketByQRCode(qrCode, eventId);
    }

    [HttpPost]
    [Route("/TIcket/Validate")]
    public async Task<bool> ValidateTicket(string qrCode, int eventId=1)
    {
        return await _ticketContext.ValidateTicket(qrCode, eventId);
    }

    [HttpPost]
   
    public async Task<FileContentResult> AddTicket(EventSalesItem ticket, string eventOrganizerName="PDAC")
    {
        Console.WriteLine(JsonSerializer.Serialize(ticket));
        ticket.TicketCode = EventUtils.PasswordGenerator.GetPassword();
        Console.WriteLine(ticket.TicketCode);

        if (await _ticketContext.AddEventTicket(ticket) >0)
        {
            var tokenReplacer = new EmailTokenReplacement();
            var values = new Dictionary<string, string>();
            // Fetch the email template
            string emailContent = await _templateAccess.GetTemplateByName("BasicEmailNew1");
            byte[] qrBytes = QRCodeUtils.GetQRCodes(ticket.TicketCode);
            
            EventOrganizer eventOrganizer = await _eventOrganizerDBAccess.GetOrganizerByName(eventOrganizerName);
            if (eventOrganizer == null)
            {
                throw new Exception($"Organizer with name {eventOrganizerName} not found.");
            }
            Event eventContext = await  _event.GetEventById(ticket.EventId);
            if (eventContext == null)
            {
                throw new Exception($"Event with ID {ticket.EventId} not found.");
            }
            // Set values for supported tokens
            if (!string.IsNullOrWhiteSpace(emailContent))
            {
                foreach (var token in EmailTokenReplacement._supportedTokens)
                {
                    switch (token)
                    {
                        case "QRCode":
                            values[token] = ticket.TicketCode;
                            break;
                        case "QRCodeImage":
                            values[token] = System.Convert.ToBase64String(qrBytes);
                            break;
                        case "EventName":
                            values[token] = eventContext.EventName; 
                            break;
                        case "Attendee":
                            values[token] = ticket.Attendee.Name;
                            break;
                        case "EventDate":
                            values[token] = eventContext.EventDate.ToString("yyyy-MM-dd");
                            break;
                        case "EventLocation":
                            values[token] = eventContext.EventLocation ?? "Not specified";
                            break;
                        case "EventOrganizerName":
                            values[token] = eventOrganizer.OrganizerName ?? "Not specified";
                            break;
                        case "EventOrganizerHelpLine":
                            values[token] = eventOrganizer.OrganizerPhone ?? "Not specified";
                            break;
                        default:
                            break;
                           
                    }
                }
                emailContent = tokenReplacer.ReplaceTokens(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent)), values);
            }

            await _sqsClient.QueueEmailMessage("support@polkadotsandcurry.com", "info@polkadotsandcurry.com", "Test Hello", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(emailContent)), ticket.Attendee.Name);
            return File(qrBytes, "image/jpeg", ticket.TicketCode);
        }
        //return QRCodeUtils.GetQRCodes(ticket.TicketCode);
        else
        {
            return File(System.IO.File.ReadAllBytes("notfound.png"), "image/jpeg");
        }
        
    }

}
