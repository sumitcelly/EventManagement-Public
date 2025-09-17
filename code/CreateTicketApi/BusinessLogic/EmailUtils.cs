
using System;
using System.Collections.Generic;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Razor.TagHelpers;


namespace  CreateTicketApi.BusinessLogic;

public class EmailUtils
{

    /// <summary>
    /// Generates a list of email tokens for replacement in email templates.
    /// </summary>
    /// <returns>A list of email tokens.</returns>

    private readonly ILogger<EmailUtils> _logger;
    private readonly EventOrganizerDBAccess _eventOrganizerDBAccess;
    private readonly EventDbAccess _eventDbAccess;
    private readonly NotificationTemplateAccess _templateAccess;
    
    private readonly UserDbAccess _userDbAccess;
    private readonly SQSHelper _sqsClient;
    public EmailUtils(ILogger<EmailUtils> logger, EventOrganizerDBAccess eventOrganizerDBAccess,
                        EventDbAccess eventDbAccess, UserDbAccess userDbAccess, NotificationTemplateAccess notificationTemplateAccess
                        , SQSHelper sqsClient)
    {
        if (eventOrganizerDBAccess == null)
        {
            throw new ArgumentNullException(nameof(eventOrganizerDBAccess));
        }
        if (eventDbAccess == null)
        {
            throw new ArgumentNullException(nameof(EventDbAccess));
        }
        if (notificationTemplateAccess == null)
        {
            throw new ArgumentNullException(nameof(notificationTemplateAccess));
        }
        _eventOrganizerDBAccess = eventOrganizerDBAccess;
        _eventDbAccess = eventDbAccess;
        _templateAccess = notificationTemplateAccess;
        _userDbAccess = userDbAccess ?? throw new ArgumentNullException(nameof(userDbAccess));
        if (sqsClient == null)
        {
            throw new ArgumentNullException(nameof(sqsClient));
        }
        _sqsClient = sqsClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    
    public async Task<bool> SendOrderConfirmationEmail(SalesOrder order,EventUser attendee =null)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "No way to send email without order.");
        }
        if (attendee == null)
        {
           attendee = await _userDbAccess.GetUserById(order.UserId);
        }
        var tokenReplacer = new EmailTokenReplacement();
        var values = new Dictionary<string, string>();
        // Fetch the email template
        string emailContent = await _templateAccess.GetTemplateByName("BasicEmailNew1");
        byte[] qrBytes = QRCodeUtils.GetQRCodes(order.SalesOrderCode);

        EventOrganizer eventOrganizer = await _eventOrganizerDBAccess.GetOrganizerById(order.CustomerId);
        if (eventOrganizer == null)
        {
            throw new Exception($"Organizer with id {order.CustomerId} not found.");
        }

        Event eventObj = await _eventDbAccess.GetEventDetailsById(order.EventId);
        if (eventObj == null)
        {
            throw new Exception($"Event with ID {order.EventId} not found.");
        }
        // Set values for supported tokens
        if (!string.IsNullOrWhiteSpace(emailContent))
        {
            foreach (var token in EmailTokenReplacement._supportedTokens)
            {
                switch (token)
                {
                    case "QRCode":
                        values[token] = order.SalesOrderCode;
                        break;
                    case "QRCodeImage":
                        values[token] = System.Convert.ToBase64String(qrBytes);
                        break;
                    case "EventName":
                        values[token] = eventObj.EventName;
                        break;
                    case "Attendee":
                        values[token] = attendee.Name ?? "Not specified";
                        break;
                    case "EventDate":
                        values[token] = eventObj.EventDate.ToString("yyyy-MM-dd");
                        break;
                    case "EventLocation":
                        values[token] = eventObj.EventLocation ?? "Not specified";
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
            emailContent = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent)), values);
        }

        await _sqsClient.QueueEmailMessage(
            "support@polkadotsandcurry.com",
            "info@polkadotsandcurry.com",
            "Test Hello",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(emailContent)),
            attendee?.Name);

        return true;
    }
}