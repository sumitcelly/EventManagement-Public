using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Amazon.SQS;
using EmailSchedulerWorker.Services;
using EventManagementDbAccess;
using Stripe;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        //services.AddSingleton<IAmazonSQS, AmazonSQSClient>();
        services.AddScoped<EmailCampaignDbAccess>();
        services.AddScoped<EmailRecipientsDbAccess>();
        services.AddScoped<NotificationTemplateAccess>();
        services.AddScoped<EventDbAccess>();
        services.AddScoped<EventOrganizerDBAccess>();
        services.AddSingleton<SQSHelper>();
        services.AddDistributedMemoryCache();
        services.AddHostedService<EmailSchedulerService>();
       
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    })
    .Build()
    .Run();
