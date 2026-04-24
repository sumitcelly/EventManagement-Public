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
       
       private readonly int _orderAgeForTaxCollection = 1;
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
            _orderAgeForTaxCollection = config.GetValue<int>("Worker:OrderAgeForTaxCollectionInHours",1);
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
                    await ProcessTaxCollectionAsync(stoppingToken,_orderAgeForTaxCollection);
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
                _logger.LogError(ex, "Error cleaning up orders");
            }
        }

        private async Task ProcessTaxCollectionAsync(CancellationToken token, int hoursAgo)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                SalesOrderDbAccess _salesOrderDbAccess = scope.ServiceProvider.GetRequiredService<SalesOrderDbAccess>();
                var salesOrders = await _salesOrderDbAccess.GetSalesOrdersForTaxCollection(hoursAgo);
               
                if (salesOrders == null || salesOrders.Count == 0)
                {
                    _logger.LogInformation($"No sales orders found for tax collection from past {hoursAgo} hours.");
                    return;
                }
                StripeAccess  stripeAccess = scope.ServiceProvider.GetRequiredService<StripeAccess>();


                _logger.LogInformation($"Processing {salesOrders.Count} sales orders for tax collection.");

                foreach (var order in salesOrders)
                {
                    try
                    {
                        // TODO: Fill in your tax collection logic here
                        string description =  $"Sales Tax: {order.EventName} (Ref: {order.OrderId} with order total {order.OrderTotal})";
                        _logger.LogInformation($"Processing order {order.OrderId} with tax amount {order.SalesTax}");
                        bool result = await stripeAccess.CollectTax(order.StripeAccountId, order.PaymentIntentId, 
                                        order.SalesTax,order.OrderId,description);
                              
                        _logger.LogInformation($@"Transfer Result for order id {order.OrderId} with description
                                             {description} for stripe account {order.StripeAccountId} is {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing order {order.OrderId} for stripe account {order.StripeAccountId} for tax collection");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tax collection processing");
            }
        }
    }

}
