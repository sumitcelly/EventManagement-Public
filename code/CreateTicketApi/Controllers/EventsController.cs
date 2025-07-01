using EventManagementDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace CreateTicketApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    // private static readonly string[] Summaries = new[]
    // {
    //     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    // };

    private readonly ILogger<EventsController> _logger;
    private readonly EventDbAccess _EventDbAccess;

    private readonly TicketAccess _ticketContext;
    public EventsController(ILogger<EventsController> logger, EventDbAccess EventDbAccess, TicketAccess ticketContext)
    {
        _logger = logger;
        _EventDbAccess = EventDbAccess;
        _ticketContext = ticketContext;
    }

    [HttpGet]
    [Route("/Events/All")]
    public  async Task<List<Event>> GetEvents()
    {
        //var eventCtxt = HttpContext.RequestServices.GetService(typeof(EventDbAccess)) as EventDbAccess;
        List<Event> events = await _EventDbAccess.GetAllEvents();
        _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(events));
        return events;
    }

   

    // [HttpGet(Name = "GetWeatherForecast")]
    // public IEnumerable<WeatherForecast> Get()
    // {
        
    //     return Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //     {
    //         Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
    //         TemperatureC = Random.Shared.Next(-20, 55),
    //         Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    //     })
    //     .ToArray();
    // }

}
