using CreateTicketApi.BusinessLogic;
using CreateTicketApi.Mappers;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    private readonly EventDbAccess _eventDbAccess;

    private readonly SalesOrderDbAccess _salesOrderDbAccess;

    private readonly IConfiguration _configuration;

    private readonly AmazonS3ContentUploader _s3Uploader;
    public TicketController(ILogger<TicketController> logger,
                            TicketAccess ticketContext,
                            SalesOrderConductor conductor,
                            EventDbAccess dbAccess,
                            IConfiguration configuration,
                            AmazonS3ContentUploader s3Uploader,
                            SalesOrderDbAccess salesOrderDbAccess)
    {
        _logger = logger;
        _ticketContext = ticketContext;
        _salesOrderConductor = conductor;
        _eventDbAccess = dbAccess;
        _configuration = configuration;
        _s3Uploader = s3Uploader;
        _salesOrderDbAccess = salesOrderDbAccess;

    }

    [Authorize(Policy="OrderOwnedByUser")]
    [HttpGet("ByEventIdAndSalesOrderQrCode/{orderId}/{id}/{salesOrderQrCode}")]
    [EnableRateLimiting("ticket-reservation-policy")]
    public async Task<IActionResult> Get(int id,string salesOrderQrCode)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(salesOrderQrCode))
            return BadRequest("Id is null.");
        //Check if user owns this salesorder or not by passing it to the next method call
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized("Unable to retrieve user id");
        }

        var ticketList = await _ticketContext.GetEventTicketBySalesOrderCode(salesOrderQrCode, id);
        if (ticketList == null)
            return NotFound();
        return Ok(ticketList);
    }


     [HttpGet]
     [Route("GetPdfUrlFromEmailLink/{encryptedOrderId}")]
     [EnableRateLimiting("strict-ip-auth")]
     public async Task<IActionResult> GetPdfUrlFromEmailLink(string encryptedOrderId)
     {
        _logger.LogInformation("Received request for sales order details with email link ID: {0}", encryptedOrderId);
        if (string.IsNullOrEmpty(encryptedOrderId))
            return BadRequest("Invalid encrypted order id.");
        string decodedId = EncryptionHelper.UrlDecode(encryptedOrderId);
        _logger.LogInformation("Decoded email link ID: {0}", decodedId);
        
        if (string.IsNullOrEmpty(decodedId))
            return BadRequest("Invalid encrypted order id after decoding.");
        try
        {
            var result = await _salesOrderDbAccess.GetEmailLinkOrderDetails(decodedId);
            if (result != null)
            {
                if (result.SalesOrderStatus != SalesOrderStatus.OrderCompleted.ToString() && 
                        result.SalesOrderStatus !=  SalesOrderStatus.PaymentSucceeded.ToString() )
                {
                    _logger.LogError(@$"Sales order  with id {decodedId} is in status {result.SalesOrderStatus.ToString()}.
                                     Cannot generate pdf");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity,"Sales order in incorrect state to generate pdf");
                }
                _logger.LogInformation("Sales order details retrieved for email link ID: {0} {1}", encryptedOrderId, result.SalesOrderCode);
                
                return await GetPdfUrl(result.SalesOrderId, result.SalesOrderCode, result.EventId);
            }
            else
                return NotFound("Sales order not found for the provided email link ID.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving sales order: {ex.Message}");
        }
    }
        

     [HttpGet]
     [Route("GetPdfUrl/{orderId}/{salesOrderCode}/{eventId}")]
     [Authorize(Policy="OrderOwnedByUser")]
     [EnableRateLimiting("strict-ip-auth")]
     public async Task<IActionResult> GetPdfUrl(int orderId,string salesOrderCode, int eventId)
     {
       if(orderId <=0 || string.IsNullOrWhiteSpace(salesOrderCode) || eventId <=0)
            return BadRequest("Invalid sales order id or event id.");

        string fileName = $"Order_{salesOrderCode}_Tickets.pdf";
        EventHeader eventDetails = await _eventDbAccess.GetEventHeaderById(eventId);

        if (eventDetails == null || eventDetails.EventOrganizerId <=0)
        {
            _logger.LogWarning($"Event not found for event id: {eventId} when trying to get PDF URL for sales order code: {salesOrderCode}");
            return NotFound("Event not found for the given event id.");
        }

        ///Need to check this here to make sure that we are not returning a pdf that was generated from S3 when the
        /// order was valid but later refunded.
        var eventTickets = (await _ticketContext.GetEventTicketBySalesOrderCode(salesOrderCode, eventId))?.ToList();
        if (eventTickets == null || eventTickets.Count == 0 || 
                eventTickets.Any(t=>t.TicketStatus != TicketStatus.Live.ToString()))
        {
            _logger.LogWarning($"No tickets found or sales order in incorret status for sales order code: {salesOrderCode} and event id: {eventId} when trying to get PDF URL.");
            return UnprocessableEntity("No tickets found for the given sales order and event or tickets in incorrect status.");
        }

        string fileKey = AmazonS3ContentUploader.GetFileKey(fileName, eventDetails.EventOrganizerId, 
                AmazonS3ContentUploader.Purpose.TicketEventPdfDocument, eventId);
        _logger.LogInformation($"Generated file key {fileKey} for sales order: {salesOrderCode}, event: {eventId}, organizer: {eventDetails.EventOrganizerId}");
        if (await _s3Uploader.DoesS3ObjectExistAsync(fileKey))
        {
            return Ok(await _s3Uploader.GetPreSignedUrlTickets(fileKey, fileName));
        }

        _logger.LogInformation($"File with key {fileKey} does not exist in S3. Generating PDF for sales order: {salesOrderCode}, event: {eventId}");
      
        //todo: make sure event is not in the past before generating PDF. We can add a buffer time as well like event should be within next 1 year to avoid generating PDF for very old events by mistake.
        if (eventDetails.EventDate < DateTime.UtcNow.AddYears(-1))
        {
            _logger.LogWarning($"Event date {eventDetails.EventDate} is too far in the past for event id: {eventId} when trying to get PDF URL for sales order code: {salesOrderCode}");
            return BadRequest("Event date is too far in the past to generate PDF.");
        }
        
        string eventDate = string.Empty,eventTime =string.Empty;
        if (eventDetails.Latitude!=0 && eventDetails.Longitude!=0)
        {
            (eventDate, eventTime)= EventUtils.TimeZoneConverter.GetLocalDateTime((double)eventDetails.Latitude,(double) eventDetails.Longitude,eventDetails.EventDate);
        }
        DateTime dtStart= DateTime.Parse(eventDate+" "+eventTime);
        DateTime dtEnd=  dtStart.AddHours(eventDetails.Duration);
        string eventDateTimeRange = $"{dtStart:MMMM dd, yyyy h:mm tt} - {dtEnd:MMMM dd, yyyy h:mm tt}";
  
        TicketPdfData pdfData = PdfDataMapper.MapToTicketPdfData(eventDetails, eventTickets);
        pdfData.EventDate = eventDateTimeRange;
        byte[] pdfBytes= PdfGenerator.GenerateTicketsWithSkiaSharp(pdfData, _configuration["EmailTemplateValues:platform_name"]??"TestEvents", _configuration["BaseFrontEndUrl"]??"");
        _logger.LogInformation($"Generated ticket PDF data for sales order: {salesOrderCode}, event: {eventId}");
        bool uploadResult = false;
        using  (var stream = new MemoryStream(pdfBytes))
        {
            uploadResult =await _s3Uploader.UploadFileAsync(fileKey, stream, "application/pdf");
            _logger.LogInformation($"Uploaded ticket PDF result to S3 for sales order: {salesOrderCode}, event: {eventId}, organizer: {eventDetails.EventOrganizerId} is {uploadResult.ToString().ToUpper()}");
        }
        return uploadResult ? Ok(await _s3Uploader.GetPreSignedUrlTickets(fileKey, fileName)) : StatusCode(500, "Error uploading PDF to storage.");

     }

    [HttpPost]
    [Route("Validate/{eventId}")]
    [Authorize(Policy = "ScanningAgent")]
    [Authorize(Policy = "EventOwnedByCustomer")]
    [EnableRateLimiting("strict-ip-auth-scanner")]
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

