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
    public class EmailStatusUpdateService: BackgroundService
    {
        private readonly ILogger<EmailStatusUpdateService> _logger;
        private readonly IConfiguration _config;
   
        private readonly int _pollIntervalSeconds;
        private readonly string _connectionString;

       
        private readonly IServiceProvider _serviceProvider;
        public EmailStatusUpdateService(
            ILogger<EmailStatusUpdateService> logger,
            IConfiguration config,
            IServiceProvider serviceProvider
            )
        {
            _logger = logger;
            _config = config;
            _serviceProvider = serviceProvider;
    

            _connectionString = config.GetConnectionString("Default") 
                ?? throw new Exception("Missing MySQL connection string.");
        
            _pollIntervalSeconds = config.GetValue<int>("Worker:PollIntervalSeconds", 60);
            
            

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (LogContext.PushProperty("JobName", nameof(EmailStatusUpdateService)))
            {
                _logger.LogInformation("EmailStatusUpdate service started.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcesEmailStatusUpdateAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in email status update service.");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), stoppingToken);
                }
            }
        }

        private async Task ProcesEmailStatusUpdateAsync(CancellationToken token)
        {
            try
            {
               using var scope = _serviceProvider.CreateScope();

               SQSHelper _sqsHelper = scope.ServiceProvider.GetRequiredService<SQSHelper>();
               List<EmailStatusUpdate> updates =  await  _sqsHelper.GetEmailStatusUpdates();
               if (updates != null && updates.Count > 0)
                {
                    _logger.LogInformation($"Received {updates.Count} email status updates from SQS.");
                    List<EmailStatusUpdate> campaignUpdates= updates.Where(x=>x.MessageType=="Campaign").ToList();
                    _logger.LogInformation($"Processing {campaignUpdates.Count} campaign email status updates.");
                    if(campaignUpdates.Count > 0)
                    {
                        EmailRecipientsDbAccess recipientsDbAccess = scope.ServiceProvider.GetRequiredService<EmailRecipientsDbAccess>();
                        if (await recipientsDbAccess.BulkUpdateCampaignStatus(campaignUpdates))
                        {
                            _logger.LogInformation($"Successfully updated campaign email statuses for {campaignUpdates.Count} updates.");
                            bool resp =await _sqsHelper.DeleteMessagesFromEmailStatus(campaignUpdates.Select(x=>x.ReceiptHandle).ToList());
                            _logger.LogInformation($"Deleted {campaignUpdates.Count} messages from SQS after processing campaign email status updates. SQS delete response: {resp}");

                        }     
                    }
                    List<EmailStatusUpdate> transactions= updates.Where(x=>x.MessageType=="Transactional").ToList();
                    _logger.LogInformation($"Processing {transactions.Count} trasactions email status updates.");
                    if(transactions.Count > 0)
                    {
                        EmailTransactionLogDbAccess transactionLogDbAccess = scope.ServiceProvider.GetRequiredService<EmailTransactionLogDbAccess>();
                        if (await transactionLogDbAccess.BulkUpdateTransactionLogs(transactions))
                        {
                            _logger.LogInformation($"Successfully updated email transaction log statuses for {transactions.Count} updates.");
                            bool resp =await _sqsHelper.DeleteMessagesFromEmailStatus(transactions.Select(x=>x.ReceiptHandle).ToList());
                            _logger.LogInformation($"Deleted {transactions.Count} messages from SQS after processing transactional email status updates. SQS delete response: {resp}");
                        }
                        else
                        {
                            _logger.LogInformation("Unable to process bulk update transaction");
                        }
                    }
                }
           }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in processing email status updates.");
            }
        }
    }

}
