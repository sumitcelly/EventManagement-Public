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
using Org.BouncyCastle.Crypto.Prng;

namespace EmailSchedulerWorker.Services
{
    public class EmailSchedulerService : BackgroundService
    {
        private readonly ILogger<EmailSchedulerService> _logger;
        private readonly IConfiguration _config;
        private readonly IAmazonSQS _sqs;
        private readonly string _connectionString;
        private readonly string _queueUrl;
        private readonly int _pollIntervalSeconds;

        private readonly EmailCampaignDbAccess _emailCampaignDbAccess;
        private readonly EmailRecipientsDbAccess _emailRecipientsDbAccess;
        private readonly NotificationTemplateAccess _templateAccess;
        private readonly EventDbAccess _eventDBAccess;

        private readonly EventOrganizerDBAccess _organizerDBAccess;
        public EmailSchedulerService(
            ILogger<EmailSchedulerService> logger,
            IConfiguration config,
            IAmazonSQS sqs,
            EmailCampaignDbAccess emailCampaignDbAccess,
            EmailRecipientsDbAccess emailRecipientsDbAccess,
            NotificationTemplateAccess templateAccess,
            EventDbAccess eventDbAccess,
            EventOrganizerDBAccess eventOrganizerDBAccess)
        {
            _logger = logger;
            _config = config;
            _sqs = sqs;
            _emailCampaignDbAccess = emailCampaignDbAccess;
            _emailRecipientsDbAccess = emailRecipientsDbAccess;
            _templateAccess = templateAccess;
            _eventDBAccess = eventDbAccess;
            _organizerDBAccess = eventOrganizerDBAccess;

            _connectionString = config.GetConnectionString("MySql") 
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
                _logger.LogInformation("Checking for pending emails at {time}.", DateTime.UtcNow);
                List<EmailCampaign> campaignList = await _emailCampaignDbAccess.GetPendingCampaigns();
                foreach (var campaign in campaignList)
                {
                    _logger.LogInformation($"Processing Email Campaign ID: {campaign.Id}, template ID {campaign.TemplateId}, eventID {campaign.EventId} ");
                     campaign.Status ="InProgress";
                    await _emailCampaignDbAccess.UpdateEmailCampaignStatus(campaign.Id, campaign.Status);
                    List<EmailRecipient> recipients = await _emailRecipientsDbAccess.GetEmailRecipientsByCampaignId(campaign.Id);
                    _logger.LogInformation($"Found {recipients.Count} recipients for Campaign ID: {campaign.Id}");
                    Dictionary<string,FullNameOrder> emailSalesOrder = [];
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
                  
                    
                    Tuple<string,string> templateData = await _templateAccess.GetTemplateByIdFromDb(campaign.TemplateId);
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
                            FullNameOrder? orderEmailData = null;
                            emailSalesOrder?.TryGetValue(recipient.RecipientEmail, out orderEmailData);
                            string content = GetEmailContentToSend(emailTemplate, eventHeader, eventOrganizer,orderEmailData);

                            var payload = JsonSerializer.Serialize(new
                            {
                                to = recipient.RecipientEmail,
                                templateId = campaign.TemplateId,
                                tokenGuid = recipient.TokenGuid,
                                orderId = orderIdEventUser.ContainsKey(recipient.Id) ? orderIdEventUser[recipient.Id].OrderId : (int?)null
                            });

                            await _sqs.SendMessageAsync(new SendMessageRequest
                            {
                                QueueUrl = _queueUrl,
                                MessageBody = payload
                            }, token);

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
                FullNameOrder? orderEmailData)
        {
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = orderEmailData?.FullName,
                EventDate = eventHeader.EventDate,
                EventLocation = eventHeader.EventLocation,
                EventName = eventHeader.EventName,
                EventOrganizerHelpLine = eventOrganizer.OrganizerPhone,
                EventOrganizerName = eventOrganizer.OrganizationName,
                EventTicketLink = $"https://eventsnow/viewmytickets/{EncryptionHelper.Encrypt(orderEmailData?.SalesOrderId)}"
            });
            
            
        }

        private async Task ProcessPendingEmailsAsync(CancellationToken token)
        {
            var dueEmails = new List<EmailJob>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync(token);

                // Fetch pending emails due now
                var query = @"
                    SELECT id, recipient_email, subject, body
                    FROM email_jobs
                    WHERE status = 'pending' AND send_at_utc <= UTC_TIMESTAMP()
                    LIMIT 50;";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    dueEmails.Add(new EmailJob
                    {
                        Id = reader.GetInt64("id"),
                        RecipientEmail = reader.GetString("recipient_email"),
                        Subject = reader.GetString("subject"),
                        Body = reader.GetString("body")
                    });
                }
            }

            if (dueEmails.Count == 0)
            {
                _logger.LogInformation("No pending emails at {time}.", DateTime.UtcNow);
                return;
            }

            _logger.LogInformation("Found {count} emails to queue.", dueEmails.Count);

            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync(token);

                foreach (var email in dueEmails)
                {
                    try
                    {
                        var payload = JsonSerializer.Serialize(new
                        {
                            to = email.RecipientEmail,
                            subject = email.Subject,
                            body = email.Body
                        });

                        await _sqs.SendMessageAsync(new SendMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MessageBody = payload
                        }, token);

                        // Mark as queued
                        var update = @"
                            UPDATE email_jobs
                            SET status = 'queued', last_attempt_at = UTC_TIMESTAMP()
                            WHERE id = @id;";

                        using var updateCmd = new MySqlCommand(update, conn);
                        updateCmd.Parameters.AddWithValue("@id", email.Id);
                        await updateCmd.ExecuteNonQueryAsync(token);

                        _logger.LogInformation("Queued email {id} for {recipient}.", email.Id, email.RecipientEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to queue email {id}", email.Id);

                        // Increment retry_count
                        var failUpdate = @"
                            UPDATE email_jobs
                            SET status = 'failed', retry_count = retry_count + 1, last_attempt_at = UTC_TIMESTAMP()
                            WHERE id = @id;";

                        using var failCmd = new MySqlCommand(failUpdate, conn);
                        failCmd.Parameters.AddWithValue("@id", email.Id);
                        await failCmd.ExecuteNonQueryAsync(token);
                    }
                }
            }
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
