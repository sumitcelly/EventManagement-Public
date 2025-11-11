using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

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

        public EmailSchedulerService(
            ILogger<EmailSchedulerService> logger,
            IConfiguration config,
            IAmazonSQS sqs)
        {
            _logger = logger;
            _config = config;
            _sqs = sqs;

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
