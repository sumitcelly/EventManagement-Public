using System.Configuration;
using CreateTicketApi.Authentication;
using CreateTicketApi.BusinessLogic;
using EventManagementDbAccess;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
// Add services to the container.

builder.Services.AddControllers(x => x.Filters.Add<ApiKeyAuthFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped(typeof(TicketAccess));
builder.Services.AddScoped(typeof(EventDbAccess));
builder.Services.AddScoped(typeof(NotificationTemplateAccess));
builder.Services.AddScoped(typeof(EventOrganizerDBAccess));
builder.Services.AddScoped(typeof(AttendeeDbAccess));
builder.Services.AddScoped(typeof(EventItemTypeDbAccess));
builder.Services.AddScoped(typeof(SalesOrderDbAccess));
builder.Services.AddScoped(typeof(SalesOrderConductor));
builder.Services.AddScoped(typeof(EmailUtils));

builder.Services.AddSingleton(typeof(SQSHelper));
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

app.UseAuthorization();

app.MapControllers();

app.Run();
