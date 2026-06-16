using System;
namespace EventManagementDbAccess
{

    public class EventSalesItem
    {
        public string? TicketCode { get; set; }

        public string QRBase64Image { get; set; } = string.Empty;

        public int EventId { get; set; } = 0;

        public EventUser  User { get; set; }
         
        public EventItemType EventItemType { get; set; } = new EventItemType();

        //public int TicketScanned { get; set; } = 0;

        public int SalesOrderId { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.MinValue;

        public DateTime ModifiedAt { get; set; } = DateTime.MinValue;

        public decimal PricePaid {get;set;} = 0;

        public string? TicketStatus {get;set; }
    }

    /// <summary>
    /// there will be other statuses in the db like Abandoned, Refunded, or Timeout which is basically the sales order status 
    /// being replicated.
    /// </summary>
    public enum TicketStatus
    {
        Scanned,
        Live
    }
}