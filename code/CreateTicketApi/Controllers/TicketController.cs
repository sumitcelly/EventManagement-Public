using EventDbAccess;
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

    private readonly SQSHelper _sqsClient  =  null;
    public TicketController(IConfiguration config, ILogger<TicketController> logger, TicketAccess ticketContext, NotificationTemplateAccess templateAccess)
    {
        _logger = logger;
        _ticketContext = ticketContext;
        _templateAccess = templateAccess;
        _sqsClient = new SQSHelper(config);
    }


    [HttpPost("GetTicketCodeByQR")]
    public string GetTicketCodeByQR(byte[] qrCode)
    {
        return QRCodeUtils.GetQRText(qrCode);
    }


    [HttpGet]
    public async Task<EventTicket> GetTicketByQRCode(string qrCode, int eventId=1)
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
   
    public async Task<FileContentResult> AddTicket(EventTicket ticket)
    {
        Console.WriteLine(JsonSerializer.Serialize(ticket));
        ticket.TicketCode = EventUtils.PasswordGenerator.GetPassword();
        Console.WriteLine(ticket.TicketCode);

        if (_ticketContext.AddEventTicket(ticket))
        {
            string emailContent = await _templateAccess.GetTemplateByName("BasicEmailNew");
            await _sqsClient.QueueEmailMessage("support@polkadotsandcurry.com","info@polkadotsandcurry.com","Test Hello",emailContent, ticket.AttendeeName);
            return File(QRCodeUtils.GetQRCodes(ticket.TicketCode), "image/jpeg", ticket.TicketCode);
        }
        //return QRCodeUtils.GetQRCodes(ticket.TicketCode);
        else
        {
            return File(System.IO.File.ReadAllBytes("notfound.png"), "image/jpeg");
        }
        //var eventCtxt = HttpContext.RequestServices.GetService(typeof(EventContext)) as EventContext;
            // return _ticketContext.AddEventTicket(new EventTicket(){
            //     EventId = 1,
            //     AttendeeEmail="test17@gmail.com",
            //     AttendeeName="test77",
            //     AttendeeSms="7719898999",
            //         TicketScanned=0
            // });
    }




}
