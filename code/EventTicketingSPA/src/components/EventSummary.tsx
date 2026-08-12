import { useWatch,Control } from "react-hook-form";
import { EventHeader  } from "../types/Event";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { getEventDateWithTimezone } from "../utils/DateUtils";

export default function EventSummary({eventBasic}: {eventBasic?:EventHeader}) {

  let event:EventHeader | null = eventBasic || null;
  if (!eventBasic || eventBasic.eventId === 0) {
    event = useAppSelector((state:RootState) => state.event);
  }

  if (!event || event.eventId === 0) {
      return <div className="p-4 border rounded-lg shadow-md bg-brand-neutral text-center mb-4">No event selected</div>;
    }
  
  return (<>
            <div className="p-4 border rounded-lg shadow-md bg-brand-neutral text-center mb-4">
              <div className="text-xs font-bold mb-2">{event.eventName}</div>
              <div className="mb-1 flex justify-between space-x-4 p-2">
                <span className="font-body">{getEventDateWithTimezone((new Date(event.eventDate)).toISOString(), event.ianaTimeZone)}</span>
                <span className="font-body">{event.eventLocation} </span>
              </div>
            </div>
          </>
  );
}
