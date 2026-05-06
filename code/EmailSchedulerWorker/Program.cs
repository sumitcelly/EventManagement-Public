using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Amazon.SQS;
using EmailSchedulerWorker.Services;
using EventManagementDbAccess;
using Stripe;
using EventUtils;
using Serilog;
using Serilog.Events;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true)
              .AddEnvironmentVariables();

        if (context.HostingEnvironment.IsDevelopment())
        {
            Console.WriteLine("Env is  dev");
            // You can force a specific local profile for dev only
            Environment.SetEnvironmentVariable("AWS_PROFILE", "SC");
        }

        config.AddSystemsManager(awsConfig =>
        {
            
            awsConfig.Path = "/global/";
            awsConfig.Optional = true;
            awsConfig.ReloadAfter = TimeSpan.FromMinutes(15);
        });

        config.AddSystemsManager(awsConfig =>
        {
            awsConfig.Path = context.HostingEnvironment.IsDevelopment()? "/dev/":"/prod/";
            awsConfig.Optional = true;
            awsConfig.ReloadAfter = TimeSpan.FromMinutes(5);
        });     
        
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
        services.AddScoped<EventItemTypeDbAccess>();
        services.AddScoped<TicketAccess>();
        services.AddScoped<SalesOrderDbAccess>();
        services.AddScoped<EmailTransactionLogDbAccess>();
        services.AddSingleton<SQSHelper>();
        services.AddSingleton<StripeAccess>();
        services.AddDistributedMemoryCache();
       // services.AddHostedService<EmailSchedulerService>();
        services.AddHostedService<OrderCleanupService>();
        services.AddHostedService<EmailStatusUpdateService>();
       
    });
    Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Hide internal MS logs below Warning
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Hide WebHost logs
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "EventsWorker-.txt"), 
    rollingInterval: RollingInterval.Day
    )
    .CreateLogger();
    builder.UseSerilog();

  

   builder.Build().Run();
   
