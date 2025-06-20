using System.Configuration;
using EventDbAccess;

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

builder.Services.Add(new ServiceDescriptor(typeof(EventContext), new EventContext(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(TicketAccess), new TicketAccess(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(NotificationTemplateAccess), new NotificationTemplateAccess(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(EventOrganizerDBAccess), new EventOrganizerDBAccess(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(AttendeeDbAccess), new AttendeeDbAccess(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(SalesOrderDbAccess), new SalesOrderDbAccess(builder.Configuration.GetConnectionString("Default"))));
builder.Services.Add(new ServiceDescriptor(typeof(SQSHelper), new SQSHelper(builder.Configuration)));
builder.Services.Add(new ServiceDescriptor(typeof(SalesOrderConductor), new SalesOrderConductor(
    builder.Services.BuildServiceProvider().GetRequiredService<ILogger<SalesOrderConductor>>(),
    builder.Services.BuildServiceProvider().GetRequiredService<SalesOrderDbAccess>(),
    builder.Services.BuildServiceProvider().GetRequiredService<TicketAccess>(),
    builder.Services.BuildServiceProvider().GetRequiredService<EventOrganizerDBAccess>(),
    builder.Services.BuildServiceProvider().GetRequiredService<AttendeeDbAccess>()
)));
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
