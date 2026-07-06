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
using System.Threading.RateLimiting;
using System.Security.Claims;


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
    //Environment.SetEnvironmentVariable("AWS_PROFILE", "SC");
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



// ----------------------------------------------------------------------
// CONFIGURE COMBINED API RATE LIMITING POLICIES
// ----------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    // Global rule when any rate limit policy is breached
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("{\"error\": \"Too many requests. Please wait a moment before trying again.\"}", token);
    };

    // ==================================================================
    // POLICY 1: Public Event Browsing (IP-Based, Lenient)
    // ==================================================================
    // Scope: GET /api/events, GET /api/events/{id}
    // Why: Allows high capacity for browsing and lets search engines index pages.
    // ==================================================================
    options.AddPolicy("public-browsing", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous-public",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("reports", context=> 
       RateLimitPartition.GetConcurrencyLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous-public",
            _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit=1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })) ;

     options.AddPolicy("financial_actions", context=> 
       RateLimitPartition.GetConcurrencyLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous-public",
            _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit=0
            })) ;
    


    // ==================================================================
    // POLICY 2: Authentication / Login (IP-Based, Strict)
    // ==================================================================
    // Scope: POST /api/auth/login, POST /api/auth/register
    // Why: Blocks automated brute-force attacks and credential stuffing.
    // ==================================================================
    options.AddPolicy("strict-ip-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "auth-ip-anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    
    //give organizer more requests
    options.AddPolicy("strict-ip-auth-organizer", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "auth-ip-anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            }));

        options.AddPolicy("strict-ip-auth-scanner", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "auth-ip-anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(5),
                QueueLimit = 0
            }));

    // ==================================================================
    // POLICY 3: Checkout Session Verification (IP-Based Token Bucket)
    // ==================================================================
    // Scope: GET /api/payments/check-payment-status (Stripe callback polling)
    // Why: Uses a Token Bucket to allow short bursts of status checks while 
    //      preventing scripts from hitting Stripe's live API endpoints.
    // ==================================================================
    options.AddPolicy("status-polling", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "stripe-ip-anon",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 30,
                QueueLimit = 0,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 5
            }));

    // ==================================================================
    // POLICY 4: Order Creation (Dynamic Email / User-ID Tracker)
    // ==================================================================
    // Scope: POST /api/orders/createorder (Hybrid checkout endpoint)
    // Why: Solves the University Network bottleneck entirely by partitioning 
    //      by unique client identifiers instead of shared public IP addresses.
    // ==================================================================
    options.AddPolicy("ticket-reservation-policy", context =>
    {
        // Track 1: Authenticated Checkout Flow
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"auth-user-{userId}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        // Track 2: Guest Checkout Flow (Reads custom header passed by React)
        var buyerEmail = context.Request.Headers["X-Buyer-Email"].ToString();
        if (!string.IsNullOrEmpty(buyerEmail))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"guest-email-{buyerEmail.Trim().ToLower()}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        // Track 3: IP Fallback
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "checkout-ip-anon";
        return RateLimitPartition.GetFixedWindowLimiter($"checkout-ip-{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// Bind the configuration section
builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection(EncryptionOptions.SectionName));


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
builder.Services.AddScoped(typeof(EventOverrideBaseDbAccess));

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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
