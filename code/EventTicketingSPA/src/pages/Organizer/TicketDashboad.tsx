import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link, useParams } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { Ticket } from "../../types/Tickets";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect } from "react";
import { Progress } from "flowbite-react";
import toast, {  Toaster } from "react-hot-toast";
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import { useEventItemTypes } from "../../utils/EventItemTypesQuery";
import { AxiosError } from "axios";
// 


  
export default function TicketDashboard({eventId,isActive}: {eventId?: string, isActive?:boolean}) {
  const history = useHistory();
 
  console.log('event id from props and is active',eventId, isActive);

  const queryClient = useQueryClient();
  const event = useAppSelector((state: RootState) => state.event);

  const deleteTicket = async (eventId: string,eventItemTypeId:number) => 
  {
    try 
    {
        console.log('Deleting ticket id  for eventId',eventItemTypeId, eventId);
        // 1. Optimistically update UI
        queryClient.setQueryData(['TicketsbyEvent', eventId], (oldData: Ticket[] | undefined) => {
          console.log('old data',oldData);
          if (!oldData) return [];
          return oldData.filter(ticket => ticket.eventItemTypeId !== eventItemTypeId);
        });

        try
        {
        // 2. Make API call
         const response = await axiosClient.delete(`/eventitemtype/${eventId}/${eventItemTypeId}`);
         console.log('response from delete ticket type', response);
         if (!response || response.status!=200)
         {
            console.error('Failed to delete ticket type:', response);
            toast.error(response.statusText || "Error deleting ticket type");
         }
         else
         {
            toast.success("Deleted ticket type succefully");         
         }
        }
        catch(error:AxiosError | any){
            toast.error(error?.response?.data || "Error deleting ticket type");
            console.log("error in catch", error?.response?.data || error.message);
        }
        finally{
          await queryClient.invalidateQueries(['TicketsbyEvent', eventId]);
        };

        // 3. Invalidate to verify our optimistic update
        // This ensures our cache matches the server state
       

    } 
    catch (error) 
    {
        console.error('Failed to delete event:', error);
    // On error, refetch to restore correct state
        await queryClient.invalidateQueries(['TicketsbyEvent', eventId]);
    }
}

  const { eventItemTypeData: data, eventItemTypesLoading: isLoading } = useEventItemTypes(eventId);


  const calculateProgress =(sold:number,allowed:number)=>{
    return allowed ! >0 ? parseFloat(((sold / allowed) * 100).toFixed(2)):0;
  }

  if (isLoading) return <p>Loading...</p>;

  return (
   
    
    <div className="max-w-md mx-auto">
    {/* <Toaster position="top-right" /> */}
      <h2 className="text-xl font-semibold mb-4 text-center">Tickets for your events</h2>
      {/*does not work for som reason. the useeffect on evenmanager is not triggered*/}
      {event && !event.isLive && (
        <div className="flex flex-row mt-4">
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                onClick={()=> history.push(`/eventmanager`, { eventId: eventId, mode: 'publish' })}
              >
                Go Live!
          </button> 
        </div>
      )}

      <div className="mt-6">
        {data && data.map((ticket:Ticket) => (
          <div
            key={ticket.eventItemTypeId}
            onClick={() => history.push(`/EventManager`, { eventId: eventId, mode:`edit`, ticketId: ticket.eventItemTypeId })}
            className="border border-gray-200 rounded-lg m-4 cursor-pointer p-4 flex items-center justify-between hover:bg-gray-50"
          >
            <div className="flex flex-col items-center w-1/4 text-center">
                <div className="font-heading text-accent-color">{ticket.name}</div>
                <div className ="font-body text-secondary-color">${ticket.cost}</div>
            </div>
            <div className="flex flex-col items-center">
                <div className="font-heading text-accent-color">Available</div>
                <div className ="font-body text-secondary-color">{ticket.totalAllowed - ticket.ticketsSold}</div>
            </div>
           
                <Progress
                    progress={calculateProgress(ticket.ticketsSold, ticket.totalAllowed)}
                    progressLabelPosition="inside"
                    textLabel="Sale Progress"
                    textLabelPosition="outside"
                    size="xl"
                    labelProgress
                    labelText
                    title={calculateProgress(ticket.ticketsSold, ticket.totalAllowed).toString()+'%'}
                />       
          
            <div className="flex flex-col gap-5">         
              <div onClick={(e)=>e.stopPropagation()}>
                <ListMenu
                  linkData={{
                    viewLink: `/EventManager`,
                    viewData: { eventId: eventId, mode:`edit`, ticketId: ticket.eventItemTypeId },
                    editLink: `/EventManager`,
                    editData: { eventId: eventId, mode:`edit`, ticketId: ticket.eventItemTypeId },
                    delete:()=>eventId && ticket.ticketsSold === 0 ? deleteTicket(eventId, ticket.eventItemTypeId) : undefined,
                    deleteEnabled: ticket.ticketsSold === 0,
                  }}
                />
              </div>
            </div>
      </div>
      
  ))}
  </div>
    <div className="flex flex-row m-4">
        <button
              className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
              onClick={()=>{ 
                history.push(`/eventmanager`, { eventId: eventId, mode: 'new' });}}
            >
              New Ticket
        </button> 
      </div>
    </div>
    
  );
  
}
