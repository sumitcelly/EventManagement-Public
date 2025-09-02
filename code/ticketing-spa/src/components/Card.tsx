import { Card } from "flowbite-react";
import { EventSearchResult } from "../pages/SearchEvents";
    import { useNavigate } from "react-router-dom";


export function EventCard({event}: 
    {event: EventSearchResult}) {
    const navigate = useNavigate();
  return (

    <Card     
      className="bg-brand-light max-w-sm cursor-pointer hover:shadow-lg"
      imgAlt="test"
      imgSrc={event.eventImageUrl}
      onClick={() => navigate(`/eventDetails/${event.eventId}`)}
    >
        <h5 className="text-2xl font-heading tracking-tight dark:text-white">
       {event.eventHeadline}
       
      </h5>
        <div className="flex justify-between mb-2 font-body">
        <span  className="text-sm dark:text-gray-400">
            {event.eventDate.toDateString()}
        </span>
        <span >
            {event.eventLocation}
        </span>   
        </div>
    
    
      <p className="font-normal text-gray-700 dark:text-gray-400 font-body">
       {event.eventSummary}
      </p>
    </Card>
  );
}
