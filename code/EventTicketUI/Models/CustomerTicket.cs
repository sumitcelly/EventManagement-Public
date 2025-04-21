using Microsoft.AspNetCore.Mvc;

namespace EventTicket
{
    public class CustomerTicket
    {
        public string TicketCode { get; set; }

        public byte[] QRCode {get; set;}

        public string CustomerName {get; set;}

        public FileContentResult FileResult { get; set; }   
    }
}
