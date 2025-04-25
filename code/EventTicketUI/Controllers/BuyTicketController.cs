using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Drawing;
using EventTicket;
using System.Text;
using System.Net.Http.Headers;
using ApiRequestModel;

public class BuyTicketController : Controller
{
    // 
    // GET: /HelloWorld/


    [HttpGet]
    public ActionResult Create()
    {
        return View();
    }
    // 
    // GET: /HelloWorld/Welcome/ 
    [HttpPost]
    public async Task<ActionResult> Create(EventTicket.Customer customerInfo)
    {
        FileContentResult result;
        byte[] imageBytes;
      
        string fileName = string.Empty;
        CustomerTicketCreate ticketCreate = new CustomerTicketCreate(){
            AttendeeName = customerInfo.AttendeeName,
             AttendeeEmail = customerInfo.AttendeeEmail,
             AttendeeSms = customerInfo.AttendeeSms,
             EventId = customerInfo.EventId,
             NumberofTickets = customerInfo.NumberofTickets
        };
        Console.WriteLine(JsonSerializer.Serialize(ticketCreate));
        using (var httpClient = new HttpClient())
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post,"http://localhost:5220/Ticket");
            msg.Content = new StringContent(JsonSerializer.Serialize(ticketCreate),Encoding.UTF8, "application/json");
            //msg.Content = new StringContent("{\"attendeeName\":\"John Doe\",\"attendeeEmail\":\"fgf@jk.com\",\"attendeeSms\":\"fgf@jk.com\",\"eventId\":1}", Encoding.UTF8, "application/json");
            // msg.Content = JsonContent.Create(customerInfo);
            
            //httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.ConnectionClose = true;
            using (HttpResponseMessage response = await httpClient.SendAsync(msg))
            //using (HttpResponseMessage response = await httpClient.PostAsJsonAsync<Customer>("http://localhost:5220/Ticket",customerInfo))
            {             
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(response.Content.Headers);
                fileName = response.Content.Headers?.ContentDisposition?.FileName;
                imageBytes = await response.Content.ReadAsByteArrayAsync();
           
                result = File(imageBytes, "image/jpeg");
                
            }
        }
       
       customerInfo.QRCode = imageBytes;
       customerInfo.TicketCode = fileName;

       return View(customerInfo);


    }
}