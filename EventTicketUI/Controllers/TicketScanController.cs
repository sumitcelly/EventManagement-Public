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
using Newtonsoft.Json.Linq;

public class TicketScanController : Controller
{
    // 
    // GET: /HelloWorld/


    [HttpGet]
    public ActionResult Scan()
    {
        return View(new Customer());
    }

    [HttpPost]
    public async Task<ActionResult> Scan(Customer customer)
    {
        if (String.IsNullOrWhiteSpace(customer.TicketCode))
          throw new ArgumentException("Invalid ticket code received");

        Console.WriteLine($"received code {customer.TicketCode}");
        Customer customer1 = new Customer();
        using (var httpClient = new HttpClient())
        {

           using (HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5220/Ticket?qrCode={customer.TicketCode}"))
           {             
                Console.WriteLine(response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string resp = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response: {resp}");
                    JObject obj = JObject.Parse(resp);
                    if (obj["ticketScanned"].ToString() =="1")
                    {
                        ModelState.Clear();
                        ModelState.AddModelError("",$"Ticket with code {customer.TicketCode} has already been scanned");
                        return View(customer1);
                    }
        
                    customer1 = new Customer(){
                        TicketCode =  customer.TicketCode,
                         AttendeeEmail = obj["attendeeEmail"].ToString(),
                         AttendeeName = obj["attendeeName"].ToString(),
                         AttendeeSms = obj["attendeeSms"].ToString(),
                    };
                }
                          
           }
               
        }
        return View("CheckIn", customer1);
    }
}