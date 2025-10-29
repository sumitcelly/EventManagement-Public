using System;
namespace EventManagementDbAccess
{
    public class EventItemType
    {
        public int EventItemTypeId { get; set; }
        public string Description { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }

        public int EventId { get; set; }

        public int TotalAllowed { get; set; }

        public int MaxPerOrder { get; set; } 
        
        public int TicketsSold { get; set; }

        public DateTime SalesStartDate { get; set; }

        public DateTime SalesEndDate { get; set; }

        public DateTime TicketValidityStart { get; set; }

        public DateTime TicketValidityEnd { get; set; }

        public bool AddOn { get; set; }
    }
}