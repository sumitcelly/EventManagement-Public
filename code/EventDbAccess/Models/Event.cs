using System;
namespace EventManagementDbAccess
{
    public class Event
    {
        public int EventId { get; set; }
        public required string EventName { get; set; }

        public DateTime EventDate { get; set; }

        public required string EventDescription { get; set; }

        public int EventOrganizer { get; set; }

        public required string Category { get; set; } = string.Empty;

        public string SubCategory { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;

        public string EventAgenda { get; set; } = string.Empty;

        public required string EventHeadline { get; set; }

        public int Capacity { get; set; }

        public bool Free { get; set; } = false;

        public required string EventLocation { get; set; }

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