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
    public class OrderCleanupService: BackgroundService
    {
        private readonly ILogger<OrderCleanupService> _logger;
        private readonly IConfiguration _config;
   
        private readonly int _pollIntervalSeconds;
        private readonly string _connectionString;

        private readonly int _orderTimeoutMinutes = 10; // Default timeout
       
        private readonly IServiceProvider _serviceProvider;
        public OrderCleanupService(
            ILogger<OrderCleanupService> logger,
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
            _orderTimeoutMinutes = config.GetValue<int>("Worker:OrderTimeoutMinutes", 10);
             _logger.LogInformation($"Using order timeout  minutes of {_orderTimeoutMinutes}");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderCleanup started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOrderCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in order cleanup service.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), stoppingToken);
            }
        }

        private async Task ProcessOrderCleanupAsync(CancellationToken token)
        {
            try
            {
               using var scope = _serviceProvider.CreateScope();

               EventItemTypeDbAccess _eventItemTypeDbAccess = scope.ServiceProvider.GetRequiredService<EventItemTypeDbAccess>();
               TicketAccess ticketAccess = scope.ServiceProvider.GetRequiredService<TicketAccess>();
               SalesOrderDbAccess _salesOrderDbAccess = scope.ServiceProvider.GetRequiredService<SalesOrderDbAccess>();
               await _salesOrderDbAccess.MarkAllReservedOrdersAsAbandoned(_orderTimeoutMinutes);
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up ordes");
            }
        }
    }

}
