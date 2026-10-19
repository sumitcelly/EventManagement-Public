
import { EventHeader } from "../types/Event";
import { SalesOrderErrors } from "../types/Order";
import { Ticket } from "../types/Tickets";


export default function SalesOrderTicket({ eventBasic, tickets, errorTicketList, salesOrderCode,qrBase64String }: {eventBasic:EventHeader, tickets:Ticket[], 
          errorTicketList:SalesOrderErrors[], salesOrderCode:string, qrBase64String:string}) {
            console.log('even basic and tickets in sales order ticket component', eventBasic);
  const dateStr = typeof eventBasic.eventDate === 'string' ? eventBasic.eventDate : eventBasic.eventDate.toISOString();
  const eventDate = new Date(dateStr.endsWith('Z') ? dateStr : dateStr + 'Z');

  return (<>
            <div className="flex flex-col items-center p-3 border border-gray-300 rounded-lg shadow-md bg-brand-light">
              <div className="mb-2 text-center text-sm font-bold font-accent">{eventBasic.eventName}</div>

              <div className="mb-3 w-full text-center text-xs text-secondary-color">
                <div className="font-bold">
                  {eventDate.toLocaleDateString()} at {eventDate.toLocaleTimeString()}
                </div>
                <div className="mt-1 font-bold">{eventBasic.eventLocation}</div>
              </div>

              <img
                src={"data:image/png;base64, " + qrBase64String}
                alt={salesOrderCode}
                className="h-auto w-full max-w-sm rounded-lg"
              />

              <div className="mt-2 text-center text-xs text-secondary-color">
                {tickets?.filter(list => list.quantity > 0)
                  .filter(list => !errorTicketList.some(e => e.eventItemTypeId === list.eventItemTypeId))
                  .map(item => (
                    <div key={item.eventItemTypeId}>
                      {item.quantity} {item.name}
                    </div>
                  ))}
              </div>
            </div>
          </>
  );
  
}
