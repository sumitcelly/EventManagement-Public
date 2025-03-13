using System;
namespace EventDbAccess
{
    public class Event
    {
        public int EventId { get; set;}
        public string EventName { get; set;}

        public DateTime EventDate {get;set;}

        public string EventDescription { get; set;} 

        public string EventOrganizer {get; set;}
    }
}