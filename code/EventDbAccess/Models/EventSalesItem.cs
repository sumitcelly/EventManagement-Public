using System;
namespace EventDbAccess
{
    public class EventSalesItem
    {
        public string TicketCode { get; set; } = string.Empty;

        public int EventId { get; set; } = 0;

        public Attendee Attendee { get; set; }

   
        public int TicketScanned { get; set; } = 0;

        public int TicketTypeId { get; set; } = 0;

        public int SalesOrderId { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}