using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Mysqlx.Crud;
using Org.BouncyCastle.Crypto.Prng;
using System.Collections.Generic;
using Serilog.Context;

namespace EmailSchedulerWorker.Services
{
    public class EmailSchedulerService : BackgroundService
    {
        private readonly ILogger<EmailSchedulerService> _logger;
        private readonly IConfiguration _config;
        private readonly string _connectionString;
      
        private readonly int _pollIntervalSeconds;

        private readonly SQSHelper _sqsClient;
        private readonly IServiceProvider _serviceProvider;
        public EmailSchedulerService(
            ILogger<EmailSchedulerService> logger,
            IConfiguration config,
            IServiceProvider serviceProvider,
            SQSHelper sqsClient)
        {
            _logger = logger;
            _config = config;
            _serviceProvider = serviceProvider;
        
            _sqsClient = sqsClient;

            _connectionString = config.GetConnectionString("Default") 
                ?? throw new Exception("Missing MySQL connection string.");
           
            _pollIntervalSeconds = config.GetValue<int>("Worker:PollIntervalSeconds", 60);
            logger.LogInformation("Starting email scheduler job");
            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (LogContext.PushProperty("JobName", nameof(EmailSchedulerService)))
            {
                _logger.LogInformation("EmailSchedulerWorker started.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessPendingEmailsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in processing emails.");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), stoppingToken);
                }
            }
        }

        private async Task ProcessPendingEmailsAsync(CancellationToken token)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                EmailCampaignDbAccess _emailCampaignDbAccess = scope.ServiceProvider.GetRequiredService<EmailCampaignDbAccess>();
                EmailRecipientsDbAccess _emailRecipientsDbAccess = scope.ServiceProvider.GetRequiredService<EmailRecipientsDbAccess>();
                NotificationTemplateAccess _templateAccess = scope.ServiceProvider.GetRequiredService<NotificationTemplateAccess>();
                EventDbAccess _eventDBAccess = scope.ServiceProvider.GetRequiredService<EventDbAccess>();
                EventOrganizerDBAccess _organizerDBAccess = scope.ServiceProvider.GetRequiredService<EventOrganizerDBAccess>();
                
                _logger.LogInformation("Checking for pending/inprogress campaigns at {time}.", DateTime.UtcNow);
                List<EmailCampaign> campaignList = await _emailCampaignDbAccess.GetPendingCampaigns();
                _logger.LogInformation($"Found {campaignList.Count} pending campaigns to process at {DateTime.UtcNow}.");
                foreach (var campaign in campaignList)
                {
                    _logger.LogInformation($"Processing Email Campaign ID: {campaign.Id}, template ID {campaign.TemplateId}, eventID {campaign.EventId} with status {campaign.Status}");
                    
                    await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "InProgress");
                    List<EmailRecipient> recipients = new List<EmailRecipient>();
                    if (campaign.Status == "Incomplete")
                    {
                        _logger.LogInformation($"Resuming processing of incomplete campaign ID: {campaign.Id}");
                        recipients = await _emailRecipientsDbAccess.GetEmailRecipientsByCampaignId(campaign.Id);
                        _logger.LogInformation($"Found {recipients.Count} recipients for Campaign ID: {campaign.Id}");
                        if (recipients.Count == 0)
                        {
                            _logger.LogWarning($"No recipients found for incomplete Campaign ID: {campaign.Id}. Marking campaign as NoRecipients.");
                            await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "NoRecipients");
                            continue;
                        }
                    }                    
                    
                    campaign.Status ="InProgress";

                    List<OrderEmailDetails> emailSalesOrder = [];
                    if (recipients.Count == 0)
                    {
                        _logger.LogInformation(@$"No recipients found for Campaign ID: {campaign.Id}. 
                            Checking for event attendees for event ID {campaign.EventId}...");
                        if (campaign.EventId.HasValue)
                        {
                            emailSalesOrder = await _emailRecipientsDbAccess.InsertRecipientsForEvent(campaign.EventId.Value, campaign.Id);
                            _logger.LogInformation($"Found {emailSalesOrder.Count} event attendees for Event ID: {campaign.EventId}");
                            if (emailSalesOrder.Count > 0)
                            {
                                _logger.LogInformation($" Recipients inserted for Campaign ID: {campaign.Id} from Event ID: {campaign.EventId}");
                                recipients = await _emailRecipientsDbAccess.GetEmailRecipientsByCampaignId(campaign.Id);
                            }
                            else
                            {
                                _logger.LogWarning($"No event attendees found for Event ID: {campaign.EventId}. Skipping campaign.");
                                await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "NoRecipients");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Campaign ID: {campaign.Id} is not associated with any event. Skipping campaign.");
                            await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "NoRecipients");
                            continue;
                        }
                    }
                    
                    EventHeader? eventHeader = null;
                    EventOrganizer? eventOrganizer = null;
                    if (campaign.EventId.HasValue)
                    {
                        eventHeader = await _eventDBAccess.GetEventHeaderById(campaign.EventId.Value);
                        if (eventHeader != null && eventHeader.EventOrganizerId > 0)
                        {
                            _logger.LogInformation($"Found event info for event {campaign.EventId} for campaign id {campaign.Id}");
                            eventOrganizer = await _organizerDBAccess.GetOrganizerById(eventHeader.EventOrganizerId);
                            if (eventOrganizer != null)
                                _logger.LogInformation($"Found organizer info for event {campaign.EventId}");
                            else
                                _logger.LogError($"Unable to find org info for even {campaign.EventId}");

                        }
                        else
                        {
                            _logger.LogError($@"No event information found for event ID {campaign.EventId} 
                                                while processing {campaign.Id}");
                        }
                    }
                    if (eventHeader == null || eventOrganizer == null)
                    {
                        _logger.LogError($@"No event information found for event ID {campaign.EventId} or no organizer info 
                                            found for {eventHeader?.EventOrganizerId} while processing {campaign.Id}");
                        await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "MissingData");
                        continue;
                    }
                  
                    
                    Tuple<string,string,bool> templateData = await _templateAccess.GetTemplateById(campaign.TemplateId);
                    string emailTemplate = templateData.Item1;
                    string emailSubject = templateData.Item2;
                    if (string.IsNullOrWhiteSpace(emailTemplate))
                    {
                        _logger.LogError($"Could not find template to send email for template id {campaign.TemplateId}");
                        await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "MissingData");
                        continue;
                    }
                    // At this point, we have recipients to process
                    ///At this point we have all the information to send the email.
                    /// 
                    bool queueingFailure = false;
                    foreach (var recipient in recipients)
                    {
                        try
                        {                    
                            OrderEmailDetails? orderEmailDetails =  emailSalesOrder?.Where(eso=>eso.SalesOrderId==recipient.SalesOrderId).FirstOrDefault();
                           // If the template is marked as default (Item3 of the tuple), we will send the template content as is without token replacement.
                           string finalEmailContent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailTemplate));
                           string finalSubject = emailSubject;
                           if (templateData.Item3)
                                (finalEmailContent, finalSubject) =  GetEmailContentToSend(emailTemplate,emailSubject,  eventHeader, eventOrganizer,orderEmailDetails);
              

                            /*
                            Batching: Use SQS Action Batching to send up to 10 messages in a single API call from your worker to reduce costs and increase throughput.
                                Visibility Timeout: Ensure your SQS Visibility Timeout is set longer than the time it takes to actually send the email to avoid duplicate sends.
                                Idempotency: Make your sending logic idempotent so that if a message is accidentally processed twice, the user doesn't receive the same campaign email twice. 
                                Amazon Web Services
                                Amazon Web Services
                            +3*/       
                                       
                            QueueResponse resp= await _sqsClient.QueueEmailMessage(
                               _config["FromEmail"] ?? throw new Exception("Missing FromEmail configuration."),
                                recipient.RecipientEmail,//"info@polkadotsandcurry.com",//attendee.Email,
                                finalSubject,
                                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(finalEmailContent)),
                                string.Empty,
                                recipient.Id,
                                "Campaign");
                           
                            // Update recipient status to 'Queued'
                            queueingFailure = queueingFailure || resp.Retry;
                            recipient.Status = resp.Success ? "Queued" : (resp.Retry ? "QueuingFailure_Retry" : "QueuingFailure_NoRetry");
                            recipient.ErrorMessage = resp.Success ? string.Empty : resp.Message;
                            recipient.LastAttemptedAt = DateTime.UtcNow;
                            await _emailRecipientsDbAccess.UpdateEmailRecipient(recipient);
                            
                            if (resp.Success)
                             _logger.LogInformation($"Queued email for Recipient ID: {recipient.Id}, Email: {recipient.RecipientEmail}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to queue email for Recipient ID: {recipient.Id}, Email: {recipient.RecipientEmail}");

                            // Update recipient status to 'Failed' and increment retry count
                            recipient.Status = "QueuingFailure_NoRetry";
                           // recipient.RetryCount = (recipient.RetryCount ?? 0) + 1;
                            recipient.LastAttemptedAt = DateTime.UtcNow;
                            recipient.ErrorMessage = ex.Message;
                            await _emailRecipientsDbAccess.UpdateEmailRecipient(recipient);
                            //not sure if we want to mark the whole campaign as failed if any recipient fails,
                            //  for now we will just log the error and move on to the next recipient
                            //await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, "Incomplete");
                        }
                    }
                   
                    //The errors should really only be from transient issues with SQS or the email service, so retrying in the next run should be sufficient.
                    await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, queueingFailure?"Incomplete": "Completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending emails.");
            }
        }

        private (string,string) GetEmailContentToSend(string emailTemplate, string subject, EventHeader eventHeader, EventOrganizer eventOrganizer,
                OrderEmailDetails? orderEmailData)
        {          
            string eventDate = string.Empty,eventTime =string.Empty;
            if (eventHeader.Latitude!=0 && eventHeader.Longitude!=0)
            {
                (eventDate, eventTime)= EventUtils.TimeZoneConverter.GetLocalDateTime((double)eventHeader.Latitude,(double) eventHeader.Longitude,eventHeader.EventDate);
            }
           
            //(string date,string time)= EventUtils.TimeZoneConverter.GetLocalDateTime(eventHeader.la);
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = orderEmailData?.FullName ?? "Attendee",
                EventLocalDate = eventDate,
                EventLocalTime = eventTime,
                EventLocation = eventHeader.EventLocation,
                VenueName = " ",
                EventName = eventHeader.EventName,
                EventOrganizerEmail = eventOrganizer.OrganizerEmail,
                EventOrganizerName = eventOrganizer.OrganizationName,
                EventTicketLink = orderEmailData?.SalesOrderId>0?
                                    $"{_config["BaseUrl"]}/ticketdetails/{EncryptionHelper.Encrypt(orderEmailData.SalesOrderId.ToString(),_config["Encryption:Secretkey"]??string.Empty)}"
                                    :string.Empty
            });
            
            var tokenReplacer = new EmailTokenReplacement(_config);
            string replacedSubject = tokenReplacer.ReplaceEventNameInSubject(subject,eventHeader.EventName);
            return (tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailTemplate)), values),replacedSubject);        
        }

    }

    public class EmailJob
    {
        public long Id { get; set; }
        public string RecipientEmail { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
    }
}
