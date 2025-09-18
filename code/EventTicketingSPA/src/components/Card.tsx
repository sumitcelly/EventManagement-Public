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
      <div className="text-xl font-heading tracking-tight dark:text-white">
       {event.eventName}   
      </div>
      <div className="text-l font-heading tracking-tight dark:text-white">
       {event.eventHeadline}   
      </div>
      <div className="flex flex-row mb-2 font-body text-sm">
        <div  className="dark:text-gray-400 mr-4">
            {event.eventDate && new Date(event.eventDate).toLocaleDateString()}
        </div>
        <div >
            {event.eventLocation}
        </div>   
      </div>
      <p className="font-normal text-gray-700 dark:text-gray-400 font-body">
       {event.eventSummary}
      </p>
    </Card>
  );
}
