using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;

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
            policy.WithOrigins("http://localhost:5173") // your React dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // needed if sending cookies
        });
});
// Add services to the container.
builder.Services.AddControllers();
//not sure if this will work with swagger, but it is needed for JWT authentication

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
builder.Services.AddAuthorization();

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
