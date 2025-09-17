using System;
using Stripe;
namespace EventManagementDbAccess
{
    public class EventHeader
    {
        public int EventId { get; set; }
        public required string EventName { get; set; }

        public DateTime EventDate { get; set; }

        public required string EventSummary { get; set; }

        public string EventOrganizer { get; set; } = string.Empty;

        public required int EventOrganizerId { get; set; }

        public required string EventLocation { get; set; }

        public required string EventHeadline { get; set; }

        public bool Free { get; set; } = false;

        
    }
    public class Event :EventHeader
    {
      
        public required string EventDescription { get; set; }

        public required string Category { get; set; } = string.Empty;

        public string SubCategory { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;

        public string EventAgenda { get; set; } = string.Empty;


        public int Capacity { get; set; }

        public required string StreetAddress { get; set; }
        public required string City { get; set; }

        public required string State { get; set; }

        public required string ZipCode { get; set; }
        public required string Country { get; set; } = "USA";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}