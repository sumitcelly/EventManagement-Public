using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

    [Authorize]
    [HttpGet("ByEventIdAndSalesOrderQrCode/{id}/{salesOrderQrCode}")]
    public async Task<IActionResult> Get(int id,string salesOrderQrCode)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(salesOrderQrCode))
            return BadRequest("Id is null.");
        //Check if user owns this salesorder or not by passing it to the next method call
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized("Unable to retrieve user id");
        }

        var order = await _salesOrderConductor.GetSalesOrderByQrCode(id, salesOrderQrCode,userId);
        if (order == null)
            return NotFound();
        return Ok(order);
    }

    // [HttpGet]
    // public async Task<EventSalesItem> GetTicketByQRCode(string qrCode, int eventId)
    // {
    //     return await _ticketContext.GetEventTicketByQRCode(qrCode, eventId);
    // }

    [HttpPost]
    [Route("Validate/{eventId}")]
    [Authorize(Policy = "ScanningAgent")]
    [Authorize(Policy = "EventOwnedByCustomer")]
    public async Task<string> ValidateTicket(int eventId,[FromBody]ScanData data)
    {
        _logger.LogInformation($"ValidateTicket called with eventId: {eventId} and qrCode: {data.qrCode}");
        return await _ticketContext.ValidateTicket(data.qrCode, eventId);
    }

    
}

public class ScanData
{
    //public int eventId { get; set;}  
    public required string qrCode { get; set;}
}

