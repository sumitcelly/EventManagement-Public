import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect } from "react";
// 


  
export default function Dashboard() {
  const navigate = useNavigate();
  const user = useAppSelector((state: RootState) => state.auth);
  const userId = user.user?.id;
  const queryClient = useQueryClient();
  
const deleteEvent = async (eventId: number) => {
  try {

    console.log('Deleting event', eventId);
    // 1. Optimistically update UI
    queryClient.setQueryData(['EventsByOrganizer', userId], (oldData: EventHeader[] | undefined) => {
      if (!oldData) return [];
      return oldData.filter(event => event.eventId !== eventId);
    });

    // 2. Make API call
    //await axiosClient.delete(`/events/${eventId}`);

    // 3. Invalidate to verify our optimistic update
    // This ensures our cache matches the server state
    await queryClient.invalidateQueries(['EventsByOrganizer', userId]);

  } catch (error) {
    console.error('Failed to delete event:', error);
    // On error, refetch to restore correct state
    await queryClient.invalidateQueries(['EventsByOrganizer', userId]);
  }
}

  const { data, isLoading } = 
  useQuery(['EventsByOrganizer',userId], async () => {
      console.log("Fetching orders for user", userId);
      //const res = await axiosClient.get(`/SalesOrder/ByUserId/${userId}`);
      //console.log('orders fetched from backend',res.data);
      let data:EventHeader[]=[];
      data.push({
        eventName: "Food Festival",
        eventId:1,
        eventLocation:"123 Colorado Springs, CO -80920",
        eventDate:new Date("2025-12-25T10:00:00"),
        eventOrganizerId:1,
        isLive:true
      }
      ,
    {
        eventId:2,
        eventName: "Drinks Festival",
        eventLocation:"567 Colorado Springs, CO -80920",
        eventDate:new Date("2026-12-25T10:00:00"),
        eventOrganizerId:1,
        isLive:false
      });
      return data;
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes

      // refetchOnMount: false,      // don’t always re-fetch on mount
      // refetchOnWindowFocus: false,
      // refetchOnReconnect: false,
      //enabled: !!userId //  only run query if we have an id
    }
  );



  if (isLoading) return <p>Loading...</p>;
  //console.log("Fetching orders for user",userId);
  return (
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">Events you are planning</h2>
      <div className="divide-y">
        {data && data.map((event) => (
          <div
            key={event.eventId}
            onClick={() => navigate(`/eventdetails/${event.eventId}`)}
            className="border border-gray-200 rounded-lg  cursor-pointer p-4 flex items-center justify-between hover:bg-gray-50"
          >
            <div>
              <p className="font-heading text-accent-color">{event.eventName}</p>
              <div className="text-primary-color text-lg">
                <div>{new Date(event.eventDate).toLocaleDateString()}</div>
                <div>{event.eventLocation}</div>
              </div>
            </div>

            <div className="flex flex-col gap-5">
              {event.isLive ? (
                <div className="text-2xl font-accent text-go-color">Live</div>
              ) : (
                <div className="text-xl font-accent text-accent-color">Draft</div>
              )}
              <div onClick={(e)=>e.stopPropagation()}>
                <ListMenu
                  linkData={{
                    viewLink: `/eventdetails/${event.eventId}`,
                    editLink: `/ManageEvent/${event.eventId}`,
                    deleteEvent:()=>deleteEvent(event.eventId)
                  }}
                />
              </div>
            </div>
      </div>
  ))}
</div>

    </div>
  );
}
