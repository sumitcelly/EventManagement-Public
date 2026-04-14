
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
    private readonly EmailTransactionLogDbAccess _emailTransactionLogDbAccess;
    public EmailUtils(ILogger<EmailUtils> logger, EventOrganizerDBAccess eventOrganizerDBAccess,
                        EventDbAccess eventDbAccess, UserDbAccess userDbAccess, NotificationTemplateAccess notificationTemplateAccess
                        , SQSHelper sqsClient,
                        EmailTransactionLogDbAccess emailTransactionLogDbAccess)
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
        _emailTransactionLogDbAccess = emailTransactionLogDbAccess ?? throw new ArgumentNullException(nameof(emailTransactionLogDbAccess));
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
        Tuple<string,string> emailContent = await _templateAccess.GetDefaultTemplateDetailsByName(NotificationTemplateAccess.OrderConfirmationTemplateName);
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
        string replacedContent = string.Empty;
        // Set values for supported tokens
        if (!string.IsNullOrWhiteSpace(emailContent.Item1))
        {
            values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = attendee.Name,
                EventDate = eventObj.EventDate,
                EventLocation = eventObj.EventLocation,
                EventOrganizerHelpLine = eventOrganizer.OrganizerPhone,
                EventOrganizerName = eventOrganizer.OrganizationName,
                QRCode = order.SalesOrderCode,
                QRCodeImage = System.Convert.ToBase64String(qrBytes)
            });
            
            replacedContent = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent.Item1)), values);
        }

        await _sqsClient.QueueEmailMessage(
            "support@polkadotsandcurry.com",//from config
            attendee.Email,
            emailContent.Item2,
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(replacedContent)),
            attendee?.Name ?? string.Empty,
            await _emailTransactionLogDbAccess.InsertEmailTransactionLog(new EmailTransactionLog()
            {
                RecipientEmail = attendee?.Email ?? string.Empty ,
                RefId =  order.OrderId,
                EmailType = "OrderConfirmation",           
            })
            );

        return true;
    }
}