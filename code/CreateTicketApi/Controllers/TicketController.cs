using EventDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace CreateTicketApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TicketController : ControllerBase
{
    private readonly ILogger<TicketController> _logger;

    private readonly TicketAccess _ticketContext;
    public TicketController(ILogger<TicketController> logger, TicketAccess ticketContext)
    {
        _logger = logger;
        _ticketContext = ticketContext;
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
   
    public FileContentResult AddTicket(EventTicket ticket)
    {
        Console.WriteLine(JsonSerializer.Serialize(ticket));
        ticket.TicketCode = EventUtils.PasswordGenerator.GetPassword();
        Console.WriteLine(ticket.TicketCode);
        
        if ( _ticketContext.AddEventTicket(ticket))
            return File(QRCodeUtils.GetQRCodes(ticket.TicketCode),"image/jpeg",ticket.TicketCode);
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
