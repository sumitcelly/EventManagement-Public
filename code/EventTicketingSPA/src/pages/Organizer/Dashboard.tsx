import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";   
import { useEffect } from "react";
import { resetEvent, updateEvent } from "../../features/auth/eventSlice";
import { useDispatch } from "react-redux";
import { resetCart } from "../../features/auth/cartSlice";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";
// 


  
export default function Dashboard() {
  const history = useHistory();
  const user = useAppSelector((state: RootState) => state.auth);
  const customerId = user.user?.customerId;
  const queryClient = useQueryClient();
  const event = useAppSelector((state: RootState) => state.event);
  const dispatch = useDispatch();
  
  const deleteEvent = async (eventId: number) => {
  try {

    console.log('Deleting event', eventId);
    // 1. Optimistically update UI
    queryClient.setQueryData(['EventsByOrganizer', customerId], (oldData: EventHeader[] | undefined) => {
      if (!oldData) return [];
      return oldData.filter(event => event.eventId !== eventId);
    });

    // 2. Make API call
    await axiosClient.delete(`/events/${eventId}`);

    // 3. Invalidate to verify our optimistic update
    // This ensures our cache matches the server state
    await queryClient.invalidateQueries(['EventsByOrganizer', customerId]);

  } catch (error) {
    console.error('Failed to delete event:', error);
    // On error, refetch to restore correct state
    await queryClient.invalidateQueries(['EventsByOrganizer', customerId]);
  }
}

  const { data, isLoading } = 
  useQuery(['EventsByOrganizer',customerId], async () => {
      console.log("Fetching events for customer id", customerId);
      const res = await axiosClient.get(`/Events/ByCustomer/${customerId}`);
      console.log('events for customer with status',res?.data, res?.status);

      return res?.data;
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes

      // refetchOnMount: false,      // don’t always re-fetch on mount
      // refetchOnWindowFocus: false,
      // refetchOnReconnect: false,
      enabled: !!customerId //  only run query if we have an id
    }
  );



  if (isLoading) return <p>Loading...</p>;

  return (
     <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
    <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">Events you are planning</h2>
      <div className="divide-y">
        {data && data.map((event:EventHeader) => (
          <div
            key={event.eventId}
            onClick={() => history.push(`/eventdetails/${event.eventId}`)}
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
                    editLink: `/EventManager/${event.eventId}`,
                    delete:()=>deleteEvent(event.eventId)
                  }}
                />
              </div>
            </div>
      </div>))}
    </div>
    
    <div className="flex flex-row mt-4">
        <button
              className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
              onClick={()=>{ dispatch(resetEvent()); history.push(`/eventmanager`);}}
            >
              New Event
        </button> 
      </div>
  </div>
  </IonContent>
  </IonPage>
  );
}
