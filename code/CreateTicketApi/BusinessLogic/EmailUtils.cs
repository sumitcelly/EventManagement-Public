
using System;
using System.Collections.Generic;
using System.Net;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Mvc.Diagnostics;
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

    private readonly SalesOrderDbAccess _salesOrderDbAccess;
    private IConfiguration _configuration;
    public EmailUtils(ILogger<EmailUtils> logger, EventOrganizerDBAccess eventOrganizerDBAccess,
                        EventDbAccess eventDbAccess, UserDbAccess userDbAccess, NotificationTemplateAccess notificationTemplateAccess
                        , SQSHelper sqsClient,
                        EmailTransactionLogDbAccess emailTransactionLogDbAccess,
                        SalesOrderDbAccess salesOrderDbAccess,
                        IConfiguration configuration)
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
        _configuration = configuration;
        _salesOrderDbAccess = salesOrderDbAccess ?? throw new ArgumentNullException(nameof(salesOrderDbAccess));
        _emailTransactionLogDbAccess = emailTransactionLogDbAccess ?? throw new ArgumentNullException(nameof(emailTransactionLogDbAccess));
        _userDbAccess = userDbAccess ?? throw new ArgumentNullException(nameof(userDbAccess));
        if (sqsClient == null)
        {
            throw new ArgumentNullException(nameof(sqsClient));
        }
        _sqsClient = sqsClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }
    
    public async Task<bool> SendOrderConfirmationEmail(SalesOrder? order,EventUser? attendee = null,int salesOrderId=0,
                                                        string orderTotal ="",string attendeeEmail=""
                                                        )
    {
        if (order == null && salesOrderId == 0)
        {
            throw new ArgumentNullException(nameof(order), "No way to send email without order or orderid");
        }
        if (order == null && salesOrderId > 0)
        {
            order = await _salesOrderDbAccess.GetSalesOrderById(salesOrderId);
            if (order == null)
            {
                throw new Exception($"Order with id {salesOrderId} not found.");
            }
        }

        if (attendee == null)
        {
            attendee = !string.IsNullOrWhiteSpace(attendeeEmail) ? new EventUser() { Email = attendeeEmail } : 
                         await _userDbAccess.GetUserById(order.UserId);;
        }
        
        if (attendee == null)
        {
            throw new Exception("Attendee email is required to send confirmation email.");
        }

        var tokenReplacer = new EmailTokenReplacement(_configuration);
        var values = new Dictionary<string, string>();
        // Fetch the email template
        Tuple<string,string> emailContent = await _templateAccess.GetDefaultTemplateDetailsByName(NotificationTemplateAccess.OrderConfirmationTemplateName);

        EventOrganizer eventOrganizer = await _eventOrganizerDBAccess.GetOrganizerById(order.CustomerId);
        if (eventOrganizer == null)
        {
            throw new Exception($"Organizer with id {order.CustomerId} not found.");
        }

        EventHeader eventObj = await _eventDbAccess.GetEventHeaderById(order.EventId);
        if (eventObj == null)
        {
            throw new Exception($"Event with ID {order.EventId} not found.");
        }
        string replacedContent = string.Empty, replacedSubject=string.Empty;

        string eventDate = string.Empty,eventTime =string.Empty;
        if (eventObj.Latitude!=0 && eventObj.Longitude!=0)
        {
            (eventDate, eventTime)= EventUtils.TimeZoneConverter.GetLocalDateTime((double)eventObj.Latitude,(double) eventObj.Longitude,eventObj.EventDate);
        }
        // Set values for supported tokens
        if (!string.IsNullOrWhiteSpace(emailContent.Item1))
        {
            values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
               
                EventLocalDate = eventDate,
                EventLocalTime = eventTime,
                EventName = eventObj.EventName,
                EventLocation = eventObj.EventLocation,
                EventOrganizerEmail = eventOrganizer.OrganizerEmail,
                EventOrganizerName = eventOrganizer.OrganizationName,
                VenueName= " ",
                QRCode = order.SalesOrderCode ?? string.Empty,
                GrandTotal = !string.IsNullOrWhiteSpace(orderTotal)? orderTotal : order.SalesOrderTotal.ToString("C"),
                EventTicketLink = $"{_configuration["BaseFrontEndUrl"]}/ticketdetails/{EncryptionHelper.Encrypt(order.OrderId.ToString(),_configuration["Encryption:Secretkey"]?? string.Empty)}"
            });
            
            replacedContent = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent.Item1)), values);
            replacedSubject = tokenReplacer.ReplaceEventNameInSubject(emailContent.Item2,eventObj.EventName);
        }
        
        QueueResponse resp =await _sqsClient.QueueEmailMessage(
            _configuration.GetValue<string>("FromEmail") ?? string.Empty,//from config
            attendee.Email,
           replacedSubject,
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(replacedContent)),
            attendee?.Name ?? string.Empty,
            await _emailTransactionLogDbAccess.InsertEmailTransactionLog(new EmailTransactionLog()
            {
                RecipientEmail = attendee?.Email ?? string.Empty ,
                RefId =  order.OrderId,
                EmailType = "OrderConfirmation",           
            })
            );
        
        if (!resp.Success)
        {
            string status = resp.Retry ? "QueuingFailure_Retry" : "QueuingFailure_NoRetry";
            await _emailTransactionLogDbAccess.UpdateEmailTransactionLogStatus(order.OrderId, status, resp.Message);
        }

        return true;
    }

    public async Task<bool> SendMemberInvitationEmail(int customerId, string inviterName, EventOrganizerMembers member)
    {
        EventOrganizer eventOrganizer = await _eventOrganizerDBAccess.GetOrganizerById(customerId);
        if (eventOrganizer == null)
        {
            throw new Exception($"Organizer with id {customerId} not found.");
        }
        
        var tokenReplacer = new EmailTokenReplacement(_configuration);
        var values = new Dictionary<string, string>();
        // Fetch the email template
        Tuple<string,string> emailContent = await _templateAccess.GetDefaultTemplateDetailsByName(NotificationTemplateAccess.TeamMemberInvitation);

        string replacedContent = string.Empty, replacedSubject=string.Empty;
        
        // Set values for supported tokens
        if (!string.IsNullOrWhiteSpace(emailContent.Item1))
        {
            values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                EventOrganizerName = eventOrganizer.OrganizationName,
                TeamInviterName = inviterName,
                MemberRole = member.Role,
                MemberName = member.FullName ?? string.Empty,
                InviteUrl = $"{_configuration["BaseFrontEndUrl"]}/invitationaccept?token={member.InvitationToken}"
            });
            
            replacedContent = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent.Item1)), values);
            replacedSubject = tokenReplacer.ReplaceOrganizationbNameInSubject(emailContent.Item2,eventOrganizer.OrganizationName);
        }
        
        QueueResponse resp =await _sqsClient.QueueEmailMessage(
            _configuration.GetValue<string>("FromEmail") ?? string.Empty,//from config
            member.Email,
           replacedSubject,
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(replacedContent)),
            inviterName,
            await _emailTransactionLogDbAccess.InsertEmailTransactionLog(new EmailTransactionLog()
            {
                RecipientEmail = member.Email ,
                RefId =  member.OrganizerMemberId,
                EmailType = "OrganizerMemberInvitation",           
            })
            );
        
        if (!resp.Success)
        {
            string status = resp.Retry ? "QueuingFailure_Retry" : "QueuingFailure_NoRetry";
            await _emailTransactionLogDbAccess.UpdateEmailTransactionLogStatus(member.OrganizerMemberId, status, resp.Message);
        }

        return true;
        
    }
    public async Task<bool> SendRefundConfirmationEmail(int salesOrderId, string refundAmount,
                                                        string attendeeEmail)
    {
        if (salesOrderId <=0)
        {
            _logger.LogError("Unable to refund without a sales order id");
            return false;
        }
       
        SalesOrder order = await _salesOrderDbAccess.GetSalesOrderById(salesOrderId);
        if (order == null)
        {
          _logger.LogError($"Cannot find sales order for orderId {salesOrderId}");
            return false;
        }
       
        var tokenReplacer = new EmailTokenReplacement(_configuration);
        var values = new Dictionary<string, string>();
        // Fetch the email template
        Tuple<string,string> emailContent = await _templateAccess.GetDefaultTemplateDetailsByName(NotificationTemplateAccess.RefundSuccess);

        EventOrganizer eventOrganizer = await _eventOrganizerDBAccess.GetOrganizerById(order.CustomerId);
        if (eventOrganizer == null)
        {
            throw new Exception($"Organizer with id {order.CustomerId} not found.");
        }
        EventHeader eventObj = await _eventDbAccess.GetEventHeaderById(order.EventId);
        if (eventObj == null)
        {
            throw new Exception($"Event with ID {order.EventId} not found.");
        }
        string replacedContent = string.Empty, replacedSubject=string.Empty;
        string eventDate = string.Empty,eventTime =string.Empty;
        if (eventObj.Latitude!=0 && eventObj.Longitude!=0)
        {
            (eventDate, eventTime)= EventUtils.TimeZoneConverter.GetLocalDateTime((double)eventObj.Latitude,(double) eventObj.Longitude,eventObj.EventDate);
        }
        // Set values for supported tokens
        if (!string.IsNullOrWhiteSpace(emailContent.Item1))
        {
            values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                EventLocalDate = eventDate,
                EventLocalTime = eventTime,
            
                EventName = eventObj.EventName,
                EventLocation = eventObj.EventLocation,
                EventOrganizerEmail = eventOrganizer.OrganizerEmail,
                EventOrganizerName = eventOrganizer.OrganizationName,
                VenueName= " ",
                QRCode = order.SalesOrderCode ?? string.Empty,
                RefundAmount = !string.IsNullOrWhiteSpace(refundAmount)? refundAmount : order.RefundAmount.ToString("C"),
            });
            
            replacedContent = tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailContent.Item1)), values);
            replacedSubject = tokenReplacer.ReplaceEventNameInSubject(emailContent.Item2,eventObj.EventName);
        }
        
        QueueResponse resp =await _sqsClient.QueueEmailMessage(
            _configuration.GetValue<string>("FromEmail") ?? string.Empty,//from config
            attendeeEmail,
           replacedSubject,
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(replacedContent)),
            string.Empty,
            await _emailTransactionLogDbAccess.InsertEmailTransactionLog(new EmailTransactionLog()
            {
                RecipientEmail = attendeeEmail,
                RefId =  order.OrderId,
                EmailType = "RefundSuccess",           
            })
            );
        
        if (!resp.Success)
        {
            string status = resp.Retry ? "QueuingFailure_Retry" : "QueuingFailure_NoRetry";
            await _emailTransactionLogDbAccess.UpdateEmailTransactionLogStatus(order.OrderId, status, resp.Message);
        }

        return true;
    }
}