namespace EventTicket
{
    public class Customer
    {
        public string AttendeeEmail { get; set; }

        public string AttendeeSms {get;set;}

        public string AttendeeName {get; set; }

        public int NumberofTickets {get; set;}  
        
        public int EventId {get; set;}  =1;
        
        public string TicketCode { get; set; }="";

        public byte[] QRCode {get; set;} 

        
    }
}