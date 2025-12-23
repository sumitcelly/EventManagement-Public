using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
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
    private readonly SalesOrderConductor _salesOrderConductor;


    public TicketController(ILogger<TicketController> logger,
                            TicketAccess ticketContext,
                            SalesOrderConductor conductor)
    {
        _logger = logger;
        _ticketContext = ticketContext;
        _salesOrderConductor = conductor;

    }


    [HttpPost("GetTicketCodeByBase64QR")]
    public string GetTicketCodeByBase64QR([FromBody]string qrCode)
    {

        return QRCodeUtils.GetQRText(Convert.FromBase64String(qrCode));
    }

    [HttpGet("ByEventIdAndSalesOrderQrCode/{id}/{salesOrderQrCode}")]
    public async Task<IActionResult> Get(int id,string salesOrderQrCode)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(salesOrderQrCode))
            return BadRequest("Id is null.");
        var order = await _salesOrderConductor.GetSalesOrderByQrCode(id, salesOrderQrCode);
        if (order == null)
            return NotFound();
        return Ok(order);
    }

    [HttpGet]
    public async Task<EventSalesItem> GetTicketByQRCode(string qrCode, int eventId)
    {
        return await _ticketContext.GetEventTicketByQRCode(qrCode, eventId);
    }

    [HttpPost]
    [Route("/Ticket/Validate")]
    public async Task<string> ValidateTicket(string qrCode, int eventId)
    {
        return await _ticketContext.ValidateTicket(qrCode, eventId);
    }

   

}

