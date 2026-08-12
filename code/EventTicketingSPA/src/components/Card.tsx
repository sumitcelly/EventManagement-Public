import { Card } from "flowbite-react";
import { EventSearchResult } from "../pages/SearchEvents";
import { useHistory } from "react-router-dom";
import { useIonRouter } from "@ionic/react";
import { getEventDateWithTimezone } from "../utils/DateUtils";


export function EventCard({event}: 
    {event: EventSearchResult}) {
    const ionRouter = useIonRouter();
    const history = useHistory();
   // console.log('card data',event);
  return (

    <Card     
      className="bg-brand-light max-w-xs  cursor-pointer hover:shadow-lg dark:bg-brand-neutral"
     
      //imgSrc={event.eventBannerUrl}
      onClick={() => history.push(`/eventDetails/${event.organizerUrlName}/${event.eventUrlName}`)}
    >
    
        <div className="aspect-video overflow-hidden rounded-t-lg ">
          <img 
            className="object-cover object-top w-full" 
            src={event.eventBannerUrl} 
            alt={event.eventName} 
          />
        </div>
        <div className="text-xl font-heading tracking-tight dark:text-white">
        {event.eventName}   
        </div>
        <div className="text-l font-heading tracking-tight dark:text-white">
        {event.eventHeadline}   
        </div>
        <div className="flex flex-row mb-2 font-body text-sm">
          <div  className="mr-4">
              {event.eventDate &&  getEventDateWithTimezone(event.eventDate+'Z', event.ianaTimeZone)}
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
