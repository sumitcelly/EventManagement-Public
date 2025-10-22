import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useNavigate,Link, useParams } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { Ticket } from "../../types/Tickets";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect } from "react";
import { Progress } from "flowbite-react";
// 


  
export default function TicketDashboard({eventId,isActive}: {eventId?: string, isActive?:boolean}) {
  const navigate = useNavigate();
 
  console.log('event id from props',eventId);

  const queryClient = useQueryClient();
  
  const deleteTicket = async (eventId: number,ticketId:number) => 
  {
    try 
    {
        console.log('Deleting ticket', eventId);
        // 1. Optimistically update UI
        queryClient.setQueryData(['TicketsbyEvent', eventId], (oldData: Ticket[] | undefined) => {
        if (!oldData) return [];
        return oldData.filter(ticket => ticket.eventItemTypeId !== ticketId);
        });

        // 2. Make API call
        //await axiosClient.delete(`/events/${eventId}`);

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
      //const res = await axiosClient.get(`/SalesOrder/ByUserId/${userId}`);
      //console.log('orders fetched from backend',res.data);
      let data:Ticket[]=[];
      data.push({
        eventItemTypeId:1,
        name:"General Admission",
        description:"General Admission Ticket",
        cost:50,
        maxPerOrder:10,
        ticketsSold:5,
        totalAllowed:100,
        quantity:100,
      },
    {
        eventItemTypeId:2,
        name:"VIP Admission",
        description:"VIP Admission Ticket",
        cost:100,
        maxPerOrder:0,
        ticketsSold:60,
        totalAllowed:100,
        quantity:100,
      });
      return data;
    },
    {
      //staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes

      // refetchOnMount: false,      // don’t always re-fetch on mount
      // refetchOnWindowFocus: false,
      // refetchOnReconnect: false,
      enabled: !!eventId && isActive //  only run query if we have an id
    }
  );



  if (isLoading) return <p>Loading...</p>;

  return (
    <div className="max-w-md mx-auto">
      
      <h2 className="text-xl font-semibold mb-4 text-center">Tickets for your events</h2>
      
      <div className="mt-6">
        {data && data.map((ticket) => (
          <div
            key={ticket.eventItemTypeId}
            onClick={() => navigate(`/ticketDetails/${eventId}/${ticket.eventItemTypeId}`)}
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
                    progress={ticket.totalAllowed>0 ? (ticket.ticketsSold / ticket.totalAllowed) * 100 : 0}
                    progressLabelPosition="inside"
                    textLabel="Sale Progress"
                    textLabelPosition="outside"
                    size="xl"
                    labelProgress
                    labelText
                    className=""
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
              onClick={()=> navigate(`/eventmanager/${eventId}/new`)}
            >
              New Ticket
        </button> 
      </div>
    </div>
  );
}
