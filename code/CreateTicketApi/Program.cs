using System.Configuration;
using EventDbAccess;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(typeof(TicketAccess));
builder.Services.AddScoped(typeof(EventContext));
builder.Services.AddScoped(typeof(NotificationTemplateAccess));
builder.Services.AddScoped(typeof(EventOrganizerDBAccess));
builder.Services.AddScoped(typeof(AttendeeDbAccess));
builder.Services.AddScoped(typeof(SalesOrderDbAccess));
builder.Services.AddSingleton(typeof(SQSHelper));
builder.Services.AddScoped(typeof(SalesOrderConductor));
// builder.Services.Add(new ServiceDescriptor(typeof(SalesOrderConductor), new SalesOrderConductor(
//     builder.Services.BuildServiceProvider().GetRequiredService<ILogger<SalesOrderConductor>>(),
//     builder.Services.BuildServiceProvider().GetRequiredService<SalesOrderDbAccess>(),
//     builder.Services.BuildServiceProvider().GetRequiredService<TicketAccess>(),
//     builder.Services.BuildServiceProvider().GetRequiredService<EventOrganizerDBAccess>(),
//     builder.Services.BuildServiceProvider().GetRequiredService<AttendeeDbAccess>()
// )));
var app = builder.Build();




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
