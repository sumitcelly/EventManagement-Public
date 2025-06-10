using System;
namespace EventDbAccess
{
    public class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; }

        public DateTime EventDate { get; set; }

        public string EventDescription { get; set; }

        public int EventOrganizer { get; set; }

        public string EventLocation { get; set; } = string.Empty;
    }
}