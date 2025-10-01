
import { EventHeader } from "../types/Event";
import { SalesOrderErrors } from "../types/Order";
import { Ticket } from "../types/Tickets";


export default function SalesOrderTicket({ eventBasic, tickets, errorTicketList, salesOrderCode,qrBase64String }: {eventBasic:EventHeader, tickets:Ticket[], 
          errorTicketList:SalesOrderErrors[], salesOrderCode:string, qrBase64String:string}) {

  return (<>
  
            <div className="flex flex-col p-2 border border-gray-300 rounded-lg shadow-md bg-brand-light">  
               
              <div className="text-sm text-center font-bold mb-2 font-accent">{eventBasic.eventName}</div>
         
              <div className="flex flex-row mb-2">
                <div className="w-1/2  pl-1 flex flex-col">
                  <div className="font-bold mt-2 mb-1 text-xs text-secondary-color">
                    {new Date(eventBasic.eventDate).toLocaleString()}            
                  </div>
                  <div className="font-bold mb-1 text-xs text-secondary-color">
                    {eventBasic.eventLocation}           
                  </div>
        
                
                  <div className="mt-1 text-xs text-secondary-color">
                    { tickets?.filter(list =>list.quantity>0).
                      filter(list=>!errorTicketList.some(e=>e.eventItemTypeId === list.eventItemTypeId))
                      .map((item) =>
                      (
                        
                        <div key={item.eventItemTypeId}>
                            {item.quantity} {item.name}
                        </div>                        
                        
                      ))
                    }
                  </div>
                </div>
                <div className="w-1/3 ml-auto mr-4">
                <div className="flex flex-col items-center">
                  {/* <div className="font-bold mb-1 text-xs text-secondary-color">{salesOrderCode}</div> */}
                  <img src={"data:image/png;base64, "+qrBase64String} alt={salesOrderCode} className="h-auto rounded-lg" />
                </div>
              </div>
              </div>
              
            </div>
          </>
  );
  
}
