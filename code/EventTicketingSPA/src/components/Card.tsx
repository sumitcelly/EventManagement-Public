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
      className="bg-brand-light w-full max-w-[240px] cursor-pointer rounded-lg p-2 shadow-sm hover:shadow-lg dark:bg-brand-neutral sm:max-w-xs sm:p-4"
      //imgSrc={event.eventBannerUrl}
      onClick={() => history.push(`/eventDetails/${event.organizerUrlName}/${event.eventUrlName}`)}
    >

        <div className="aspect-video overflow-hidden rounded-t-md sm:rounded-t-lg">
          <img
            className="object-cover object-top w-full"
            src={event.eventBannerUrl}
            alt={event.eventName}
          />
        </div>
        <div className="mt-2 text-base font-heading tracking-tight dark:text-white sm:text-xl">
        {event.eventName}
        </div>
        <div className="text-sm font-heading tracking-tight dark:text-white sm:text-base">
        {event.eventHeadline}
        </div>
        <div className="mt-2 space-y-1.5 font-body text-[11px] text-gray-800 dark:text-gray-200 sm:mb-2 sm:text-sm">
          <div className="font-bold text-brand-primary">
              {event.eventDate &&  getEventDateWithTimezone(event.eventDate+'Z', event.ianaTimeZone)}
          </div>
          <div className="break-words font-medium text-gray-700 dark:text-gray-200">
              {event.eventLocation}
          </div>
        </div>
        <p className="mt-1 font-normal text-gray-700 dark:text-gray-400 font-body tracking-tight text-[11px] sm:text-sm">
        {event.eventSummary}
        </p>

    </Card>
  );
}
