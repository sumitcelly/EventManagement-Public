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
        if ( Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development") 
        {
            Console.WriteLine("Env is dev");
            config.AddJsonFile("appsettings.Development.json", optional: true);
               
        }
        //only add if you want docker compose to override the default valuesa
        config.AddEnvironmentVariables();

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!= "Development")
        {
            config.AddSystemsManager(awsConfig =>
            {
                
                awsConfig.Path = "/global/";
                awsConfig.Optional = true;
                awsConfig.ReloadAfter = TimeSpan.FromMinutes(15);
            });

            config.AddSystemsManager(awsConfig =>
            {
                awsConfig.Path = "/prod/";
                awsConfig.Optional = true;
                awsConfig.ReloadAfter = TimeSpan.FromMinutes(5);
            }); 
        }    
        
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
        //services.AddDistributedMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = config.GetConnectionString("Redis");
            //use same instance as api only if you want them 2 share the data.
            //todo: may have to revisit.
            options.InstanceName = "EventsWorker_"; // Your "No. 2" prefix

        });
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

    Console.WriteLine("Env is  " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") + "");
  

   builder.Build().Run();
   
