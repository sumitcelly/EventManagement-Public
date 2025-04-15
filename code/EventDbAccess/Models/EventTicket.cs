using System;
namespace EventDbAccess
{
    public class EventTicket
    {
        public string  TicketCode {get;set;} =string.Empty;

        public int EventId { get; set;}=0;
        public string AttendeeName { get; set;}

        public string AttendeeEmail {get;set;}

        public string AttendeeSms { get; set;} 

        public int TicketScanned {get; set;} =0;
    }
}