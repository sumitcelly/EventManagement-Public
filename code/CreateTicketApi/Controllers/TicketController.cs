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
            var tokenReplacer = new EmailTokenReplacement();
            var values = new Dictionary<string, string>();
            // Fetch the email template
            string emailContent = await _templateAccess.GetTemplateByName("BasicEmailNew1");
            byte[] qrBytes = QRCodeUtils.GetQRCodes(ticket.TicketCode);
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
                            values[token] = "Name";
                            break;

                        case "Attendee":
                            values[token] = ticket.AttendeeName;
                            break;



                            // ... set other tokens as needed
                    }
                }
                emailContent = tokenReplacer.ReplaceTokens( System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent)), values);
            }

            await _sqsClient.QueueEmailMessage("support@polkadotsandcurry.com", "info@polkadotsandcurry.com", "Test Hello", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(emailContent)), ticket.AttendeeName);
            return File(qrBytes, "image/jpeg", ticket.TicketCode);
        }
        //return QRCodeUtils.GetQRCodes(ticket.TicketCode);
        else
        {
            return File(System.IO.File.ReadAllBytes("notfound.png"), "image/jpeg");
        }
        
    }

}
