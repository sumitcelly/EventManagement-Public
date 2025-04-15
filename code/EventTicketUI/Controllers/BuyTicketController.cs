using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Drawing;
using EventTicket;
using System.Text;
namespace MvcMovie.Controllers;

public class BuyTicketController : Controller
{
    // 
    // GET: /HelloWorld/


    public ActionResult Index()
    {
        return View();
    }
    // 
    // GET: /HelloWorld/Welcome/ 
    public async Task<ActionResult> Create(EventTicket.Customer customerInfo)
    {
        FileContentResult result;
        Console.WriteLine(JsonSerializer.Serialize(customerInfo));
        using (var httpClient = new HttpClient())
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post,"http://localhost:5220/Ticket");
            msg.Content = new StringContent(JsonSerializer.Serialize(customerInfo),Encoding.UTF8, "application/json");
            //msg.Content = new StringContent("{\"attendeeName\":\"John Doe\",\"attendeeEmail\":\"fgf@jk.com\",\"attendeeSms\":\"fgf@jk.com\",\"eventId\":1}", Encoding.UTF8, "application/json");
            // msg.Content = JsonContent.Create(customerInfo);
            Console.WriteLine(msg.Content.ReadAsStringAsync().Result);
            //httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.ConnectionClose = true;
            using (HttpResponseMessage response = await httpClient.SendAsync(msg))
            //using (HttpResponseMessage response = await httpClient.PostAsJsonAsync<Customer>("http://localhost:5220/Ticket",customerInfo))
            {             
                Console.WriteLine(response.StatusCode +response.ReasonPhrase);
                
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine(imageBytes);
                result = File(imageBytes, "image/png");
            }
        }

      
        return result; 
    }
}