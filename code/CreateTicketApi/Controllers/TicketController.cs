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

    [HttpPost]
   
    public string AddTicket(EventTicket ticket)
    {
        ticket.TicketCode = EventUtils.PasswordGenerator.GetPassword();
        if ( _ticketContext.AddEventTicket(ticket))
            return ticket.TicketCode;
        else
            return "FAIL";
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
