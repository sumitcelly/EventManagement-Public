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

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://10.0.2.2:5173", "https://10.0.2.2:5173", "http://localhost:5173","http://localhost", "https://localhost","https://sc-dev-ticketspro.ngrok.io") // your React dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // needed if sending cookies
        });
});
// Add services to the container.
builder.Services.AddControllers();


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
    



        // FOR LOCAL EMULATOR DEVELOPMENT:
            
    //});

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
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSwaggerGen();
//uncomment the following lines to enable API Key authentication in Swagger

// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
//     {
//         Description = "The API Key to access the API",
//         Type = SecuritySchemeType.ApiKey,
//         Name = "x-api-key",
//         In = ParameterLocation.Header,
//         Scheme = "ApiKeyScheme"
//     });

//     var scheme = new OpenApiSecurityScheme
//     {
//         Reference = new OpenApiReference
//         {
//             Type = ReferenceType.SecurityScheme,
//             Id = "ApiKey"
//         },
//         In = ParameterLocation.Header
//     };

//     var requirement = new OpenApiSecurityRequirement
//     {
//         { scheme, new string[] { } }
//     };

//     c.AddSecurityRequirement(requirement);
// });

// This registers the IHttpContextAccessor so your handler can use it
builder.Services.AddHttpContextAccessor(); 

var app = builder.Build();




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
