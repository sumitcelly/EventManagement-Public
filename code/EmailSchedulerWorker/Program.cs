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
        // Use the hosting environment provided by the Generic Host (works with DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT)
        if (context.HostingEnvironment.IsDevelopment())
        {
            Console.WriteLine("Env is Development (hosting)");
            config.AddJsonFile("appsettings.Development.json", optional: true);
        }
        //only add if you want docker compose to override the default valuesa
        config.AddEnvironmentVariables();
        //Console.WriteLine($"Redis string is {context.Configuration.GetConnectionString("Redis")}");

        if (!context.HostingEnvironment.IsDevelopment())
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
            Console.WriteLine($"Redis connection: {config.GetConnectionString("Redis")}");
           //Console.WriteLine($"Db string is {config.GetConnectionString("Default")}");
   
            //use same instance as api only if you want them 2 share the data.
            //todo: may have to revisit.
            //options.InstanceName = "EventsWorker_"; // Your "No. 2" prefix

        });
       // services.AddHostedService<EmailSchedulerService>();
        services.AddHostedService<OrderCleanupService>();
        //services.AddHostedService<EmailStatusUpdateService>();
       
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

    var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    Console.WriteLine("Env is  " + envName);
  
    builder.Build().Run();
   
