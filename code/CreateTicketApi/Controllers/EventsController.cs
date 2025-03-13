using EventDbAccess;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace CreateTicketApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<EventsController> _logger;
    private readonly EventContext _eventContext;
    public EventsController(ILogger<EventsController> logger, EventContext eventContext)
    {
        _logger = logger;
        _eventContext = eventContext;
    }

    [HttpGet]
    [Route("/Events/All")]
    public List<Event> GetEvents()
    {
        //var eventCtxt = HttpContext.RequestServices.GetService(typeof(EventContext)) as EventContext;
        List<Event> events = _eventContext.GetAllEvents();
        _logger.LogInformation("Event received are {0}", JsonSerializer.Serialize(events));
        return events;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

}
