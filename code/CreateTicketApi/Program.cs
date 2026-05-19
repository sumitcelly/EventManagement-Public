using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using Amazon.Extensions.Configuration.SystemsManager;

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddLogging(logging =>
// {
//     logging.ClearProviders();
//     logging.AddConsole();
//     logging.AddDebug();

// });

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Env is  dev");
    // You can force a specific local profile for dev only
    Environment.SetEnvironmentVariable("AWS_PROFILE", "SC");
}
else
{
    Console.WriteLine($"Env is  {builder.Environment.IsProduction()}");
}

if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddSystemsManager(config =>
    {
        config.Path = "/global/";
        config.Optional = true;
        config.ReloadAfter = TimeSpan.FromMinutes(15);
    });

    builder.Configuration.AddSystemsManager(configSource =>
    {
        configSource.Path = "/prod/";
        configSource.ReloadAfter = TimeSpan.FromMinutes(5); // Optional: How often to refresh
        configSource.Optional = true; // Optional: Don't crash if AWS is down
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://10.0.2.2:5173", "https://10.0.2.2:5173", 
                            "http://localhost:5173","http://localhost", 
                             "https://localhost","https://sc-dev-ticketspro.ngrok.io",
                            "https://dl27afdi0vyin.cloudfront.net") //Cloudfront
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // needed if sending cookies
        });
   
});
// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Dynamically prepends "/api" to every single controller route automatically
    options.Conventions.Add(new RoutePrefixConvention("api"));
});


builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("guest-checkout-policy", opt => {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5; // Allow 5 attempts per minute
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>    
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SymmetricKey"] ?? throw new ArgumentException("JWT symmetric key is not configured."))),
        };
    });
    

builder.Services.AddAuthorization(options =>
{
    // Define a policy that requires Owner level access
    options.AddPolicy("OwnerOnly", policy => 
        policy.AddRequirements(new AtleastRoleRequirement(UserRoles.Owner)));
    options.AddPolicy("FullAdminMinimum", policy => 
        policy.AddRequirements(new AtleastRoleRequirement(UserRoles.FullAdmin)));
    options.AddPolicy("RestrictedAdminMinimum", policy => 
        policy.AddRequirements(new AtleastRoleRequirement(UserRoles.RestrictedAdmin)));
    options.AddPolicy("ScanningAgent", policy => 
        policy.AddRequirements(new AtleastRoleRequirement(UserRoles.ScanningAgent)));
    
    options.AddPolicy("EventOwnedByCustomer", policy => 
        policy.Requirements.Add(new OwnerRequirement("eventId")));

    options.AddPolicy("OrderOwnedByUser", policy => 
        policy.Requirements.Add(new OwnerRequirement("orderId")));
    
    options.AddPolicy("MatchingCustomer", policy => 
        policy.AddRequirements(new CustomerIdMatchRequirement()));
    options.AddPolicy("MatchingUserId", policy => 
        policy.AddRequirements(new UserIdMatchRequirement()));
});


//builder.Services.AddControllers(x => x.Filters.Add<ApiKeyAuthFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//shoulg these be singletons?
builder.Services.AddScoped(typeof(TicketAccess));
builder.Services.AddScoped(typeof(EventDbAccess));
builder.Services.AddScoped(typeof(NotificationTemplateAccess));
builder.Services.AddScoped(typeof(EventOrganizerDBAccess));
builder.Services.AddScoped(typeof(UserDbAccess));
builder.Services.AddScoped(typeof(EventItemTypeDbAccess));
builder.Services.AddScoped(typeof(SalesOrderDbAccess));
builder.Services.AddScoped(typeof(SalesOrderConductor));
builder.Services.AddScoped(typeof(EmailUtils));
builder.Services.AddScoped(typeof(EventOrganizerMembersDbAccess));
builder.Services.AddScoped(typeof(EmailCampaignDbAccess));
builder.Services.AddScoped(typeof(EmailRecipientsDbAccess));
builder.Services.AddScoped(typeof(LoginCodesDbAccess));
builder.Services.AddScoped(typeof(EmailTransactionLogDbAccess));

// Registering as Scoped allows the injection of a Scoped DbContext
builder.Services.AddScoped<IAuthorizationHandler, GenericOwnerHandler>();

builder.Services.AddSingleton<IAuthorizationHandler, AtleastRoleHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CustomerIdMatchHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, UserIdMatchHandler>();

builder.Services.AddSingleton(typeof(RefreshTokenCache));
builder.Services.AddSingleton(typeof(JwtUtils));
builder.Services.AddSingleton<EncryptionHelper>();
builder.Services.AddSingleton(typeof(SQSHelper));
builder.Services.AddSingleton(typeof(AmazonS3ContentUploader));
builder.Services.AddSingleton(typeof(StripeAccess));
builder.Services.AddSwaggerGen();

// This registers the IHttpContextAccessor so your handler can use it
builder.Services.AddHttpContextAccessor(); 

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "EventsApi_"; // Your "No. 2" prefix

});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Hide internal MS logs below Warning
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Hide WebHost logs
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "EventsApi-.txt"), 
    rollingInterval: RollingInterval.Day
)
.CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();
app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
{
   // app.UseHttpsRedirection();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
