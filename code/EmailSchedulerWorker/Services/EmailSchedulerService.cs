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

namespace EmailSchedulerWorker.Services
{
    public class EmailSchedulerService : BackgroundService
    {
        private readonly ILogger<EmailSchedulerService> _logger;
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly string _queueUrl;
        private readonly int _pollIntervalSeconds;

        private readonly SQSHelper _sqsClient;

        private readonly EventOrganizerDBAccess _organizerDBAccess;
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
            _queueUrl = config["Sqs:QueueUrl"] 
                ?? throw new Exception("Missing SQS QueueUrl.");
            _pollIntervalSeconds = config.GetValue<int>("Worker:PollIntervalSeconds", 60);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                
                _logger.LogInformation("Checking for pending emails at {time}.", DateTime.UtcNow);
                List<EmailCampaign> campaignList = await _emailCampaignDbAccess.GetPendingCampaigns();
                foreach (var campaign in campaignList)
                {
                    _logger.LogInformation($"Processing Email Campaign ID: {campaign.Id}, template ID {campaign.TemplateId}, eventID {campaign.EventId} ");
                     campaign.Status ="InProgress";
                    await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, campaign.Status);
                    List<EmailRecipient> recipients = await _emailRecipientsDbAccess.GetEmailRecipientsByCampaignId(campaign.Id);
                    _logger.LogInformation($"Found {recipients.Count} recipients for Campaign ID: {campaign.Id}");
                    List<OrderEmailDetails> emailSalesOrder = [];

                    if (recipients.Count == 0)
                    {
                        _logger.LogWarning(@$"No recipients found for Campaign ID: {campaign.Id}. 
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
                  
                    
                    Tuple<string,string> templateData = await _templateAccess.GetTemplateById(campaign.TemplateId);
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
                    
                    foreach (var recipient in recipients)
                    {
                        try
                        {                    
                            OrderEmailDetails? orderEmailDetails =  emailSalesOrder?.Where(eso=>eso.SalesOrderId==recipient.SalesOrderId).FirstOrDefault();
                            string content = GetEmailContentToSend(emailTemplate, eventHeader, eventOrganizer,orderEmailDetails);
                                                                   
                            await _sqsClient.QueueEmailMessage(
                                "support@polkadotsandcurry.com",//from config
                                recipient.RecipientEmail,//"info@polkadotsandcurry.com",//attendee.Email,
                                emailSubject,
                                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)),
                                orderEmailDetails?.FullName ?? string.Empty);
                           
                            // Update recipient status to 'Queued'
                            recipient.Status = "Queued";
                            recipient.LastAttemptedAt = DateTime.UtcNow;
                            await _emailRecipientsDbAccess.UpdateEmailRecipient(recipient);

                            _logger.LogInformation($"Queued email for Recipient ID: {recipient.Id}, Email: {recipient.RecipientEmail}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to queue email for Recipient ID: {recipient.Id}, Email: {recipient.RecipientEmail}");

                            // Update recipient status to 'Failed' and increment retry count
                            recipient.Status = "Failed";
                            recipient.RetryCount = (recipient.RetryCount ?? 0) + 1;
                            recipient.LastAttemptedAt = DateTime.UtcNow;
                            await _emailRecipientsDbAccess.UpdateEmailRecipient(recipient);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending emails.");
            }
        }

        private string GetEmailContentToSend(string emailTemplate, EventHeader eventHeader, EventOrganizer eventOrganizer,
                OrderEmailDetails? orderEmailData)
        {
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = orderEmailData?.FullName ?? "Attendee",
                EventDate = eventHeader.EventDate,
                EventLocation = eventHeader.EventLocation,
                EventName = eventHeader.EventName,
                EventOrganizerHelpLine = eventOrganizer.OrganizerPhone,
                EventOrganizerName = eventOrganizer.OrganizationName,
                EventTicketLink = orderEmailData?.SalesOrderId>0?
                                    $"https://eventsnow/viewmytickets/{EncryptionHelper.Encrypt(orderEmailData.SalesOrderId.ToString())}"
                                    :string.Empty
            });
            
            var tokenReplacer = new EmailTokenReplacement();
            return tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailTemplate)), values);        
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
