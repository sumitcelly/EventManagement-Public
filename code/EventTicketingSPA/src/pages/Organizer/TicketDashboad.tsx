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
import AppNavbar from "../../components/Navbarnew";
// 


  
export default function TicketDashboard({eventId,isActive}: {eventId?: string, isActive?:boolean}) {
  const history = useHistory();
 
  console.log('event id from props and is active',eventId, isActive);

  const queryClient = useQueryClient();
  const event = useAppSelector((state: RootState) => state.event);

  const deleteTicket = async (eventId: number,eventItemTypeId:number) => 
  {
    try 
    {
        console.log('Deleting ticket id  for eventId',eventItemTypeId, eventId);
        // 1. Optimistically update UI
        queryClient.setQueryData(['TicketsbyEvent', eventId], (oldData: Ticket[] | undefined) => {
          if (!oldData) return [];
          return oldData.filter(ticket => ticket.eventItemTypeId !== eventItemTypeId);
        });

        // 2. Make API call
        await axiosClient.delete(`/eventitemtype/${eventId}/${eventItemTypeId}`).then((response)=>{
            if (response.data){
              toast.error(response.data);
            }
            else
            {
              toast.success("Deleted ticket type succefully");
            }
        }).
        catch((error)=>{
            toast.error("Error deleting ticket type");
            console.log("error in catch", error);
        });

        // 3. Invalidate to verify our optimistic update
        // This ensures our cache matches the server state
        await queryClient.invalidateQueries(['TicketsbyEvent', eventId]);

    } 
    catch (error) 
    {
        console.error('Failed to delete event:', error);
    // On error, refetch to restore correct state
        await queryClient.invalidateQueries(['eventId', eventId]);
    }
}

  const { data, isLoading } = 
  useQuery(['TicketsbyEvent',eventId], async () => {
      console.log("Fetching tickets for event id:", eventId);
      const res = await axiosClient.get(`/eventitemtype/all/${eventId}`);
      if (res && res.data && res.data.length>0)
      {
        return res.data;
      }
      else
      {
        return [];
      }
     
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes

      // refetchOnMount: false,      // don’t always re-fetch on mount
      // refetchOnWindowFocus: false,
      // refetchOnReconnect: false,
      enabled: !!eventId && isActive //  only run query if we have an id
    }
  );

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
                onClick={()=> history.push(`/eventmanager/${eventId}/publish`)}
              >
                Go Live!
          </button> 
        </div>
      )}

      <div className="mt-6">
        {data && data.map((ticket:Ticket) => (
          <div
            key={ticket.eventItemTypeId}
            onClick={() => history.push(`/EventManager/${eventId}/edit/${ticket.eventItemTypeId}`)}
            className="border border-gray-200 rounded-lg mt-2 cursor-pointer p-4 flex items-center justify-between hover:bg-gray-50"
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
                    viewLink: `/EventManager/${eventId}/edit/${ticket.eventItemTypeId}`,
                    editLink: `/EventManager/${eventId}/edit/${ticket.eventItemTypeId}`,
                    delete:()=>eventId ? deleteTicket(Number(eventId), ticket.eventItemTypeId) : undefined
                  }}
                />
              </div>
            </div>
      </div>
      
  ))}
  </div>
    <div className="flex flex-row mt-4">
        <button
              className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
              onClick={()=> history.push(`/eventmanager/${eventId}/new`)}
            >
              New Ticket
        </button> 
      </div>
    </div>
    
  );
  
}
