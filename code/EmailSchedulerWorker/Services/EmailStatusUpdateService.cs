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
                            _sqsHelper.DeleteMessagesFromEmailStatus(campaignUpdates.Select(x=>x.ReceiptHandle).ToList());
                        }
                    }
                    List<EmailStatusUpdate> transactions= updates.Where(x=>x.MessageType=="EmailTransactions").ToList();
                    _logger.LogInformation($"Processing {transactions.Count} trasactions email status updates.");
                    if(transactions.Count > 0)
                    {
                        EmailTransactionLogDbAccess transactionLogDbAccess = scope.ServiceProvider.GetRequiredService<EmailTransactionLogDbAccess>();
                        if (await transactionLogDbAccess.BulkUpdateTransactionLogs(transactions))
                        {
                            _logger.LogInformation($"Successfully updated email transaction log statuses for {campaignUpdates.Count} updates.");
                            _sqsHelper.DeleteMessagesFromEmailStatus(campaignUpdates.Select(x=>x.ReceiptHandle).ToList());
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
