using System;
namespace EventManagementDbAccess
{
    public class EventItemType
    {
        public int EventItemTypeId { get; set; } = 0;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Cost { get; set; } = 0.0m;
    }
    public class EventSalesItem
    {
        public string TicketCode { get; set; } = string.Empty;

        public string QRBase64Image { get; set; } = string.Empty;

        public int EventId { get; set; } = 0;

        public Attendee Attendee { get; set; }
         
        public EventItemType EventItemType { get; set; } = new EventItemType();

        public int TicketScanned { get; set; } = 0;

        public int SalesOrderId { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}