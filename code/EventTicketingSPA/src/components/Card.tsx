import { Card } from "flowbite-react";
import { EventSearchResult } from "../pages/SearchEvents";
import { useHistory } from "react-router-dom";
import { useIonRouter } from "@ionic/react";


export function EventCard({event}: 
    {event: EventSearchResult}) {
    const ionRouter = useIonRouter();
    console.log('card data',event);
  return (

    <Card     
      className="bg-brand-light max-w-xs  cursor-pointer hover:shadow-lg"
      imgAlt="test"
      imgSrc={event.eventBannerUrl}
      onClick={() => ionRouter.push(`/eventDetails/${event.organizerUrlName}/${event.eventUrlName}`)}
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
      <p className="font-normal text-gray-700 dark:text-gray-400 font-body tracking-tight">
       {event.eventSummary}
      </p>
    </Card>
  );
}
