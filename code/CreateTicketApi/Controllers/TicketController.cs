using CreateTicketApi.BusinessLogic;
using CreateTicketApi.Mappers;
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

    private readonly EventDbAccess _eventDbAccess;

    private readonly IConfiguration _configuration;

    private readonly AmazonS3ContentUploader _s3Uploader;
    public TicketController(ILogger<TicketController> logger,
                            TicketAccess ticketContext,
                            SalesOrderConductor conductor,
                            EventDbAccess dbAccess,
                            IConfiguration configuration,
                            AmazonS3ContentUploader s3Uploader)
    {
        _logger = logger;
        _ticketContext = ticketContext;
        _salesOrderConductor = conductor;
        _eventDbAccess = dbAccess;
        _configuration = configuration;
        _s3Uploader = s3Uploader;

    }


    [HttpPost("GetTicketCodeByBase64QR")]
    public string GetTicketCodeByBase64QR([FromBody]string qrCode)
    {

        return QRCodeUtils.GetQRText(Convert.FromBase64String(qrCode));
    }

    [Authorize(Policy="OrderOwnedByUser")]
    [HttpGet("ByEventIdAndSalesOrderQrCode/{orderId}/{id}/{salesOrderQrCode}")]
    public async Task<IActionResult> Get(int id,string salesOrderQrCode)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(salesOrderQrCode))
            return BadRequest("Id is null.");
        //Check if user owns this salesorder or not by passing it to the next method call
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized("Unable to retrieve user id");
        }

         var order = await _ticketContext.GetEventTicketBySalesOrderCodeFromDb(salesOrderQrCode, id);
        if (order == null)
            return NotFound();
        return Ok(order);
    }


     [HttpGet]
     [Route("GetPdfUrl/{salesOrderCode}/{eventId}")]
    // [Authorize(Policy="OrderOwnedByUser")]
     public async Task<IActionResult> GetPdfUrl(string salesOrderCode, int eventId)
     {
       if(string.IsNullOrWhiteSpace(salesOrderCode) || eventId <=0)
            return BadRequest("Invalid sales order id or event id.");

        string fileName = $"Order_{salesOrderCode}_Tickets.pdf";
        EventHeader eventDetails = await _eventDbAccess.GetEventHeaderById(eventId);

        if (eventDetails == null || eventDetails.EventOrganizerId <=0)
        {
            _logger.LogWarning($"Event not found for event id: {eventId} when trying to get PDF URL for sales order code: {salesOrderCode}");
            return NotFound("Event not found for the given event id.");
        }

        string fileKey = AmazonS3ContentUploader.GetFileKey(fileName, eventDetails.EventOrganizerId, 
                EventUtils.AmazonS3ContentUploader.Purpose.TicketEventPdfDocument, eventId);
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


        if (eventDetails == null)
        {  
            _logger.LogWarning($"Event not found for event id: {eventId} when trying to get PDF URL for sales order code: {salesOrderCode}");
            return NotFound("Event not found for the given event id.");
        }
        
        var eventTickets = (await _ticketContext.GetEventTicketBySalesOrderCodeFromDb(salesOrderCode, eventId))?.ToList();
        if (eventTickets == null || eventTickets.Count == 0)
        {
            _logger.LogWarning($"No tickets found for sales order code: {salesOrderCode} and event id: {eventId} when trying to get PDF URL.");
            return NotFound("No tickets found for the given sales order and event.");
        }
        //TODO: ensure sales order is in correct status before generating PDF. We can have a separate method to validate sales order status which can be reused in other places as well.
        
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

