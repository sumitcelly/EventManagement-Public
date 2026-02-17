import { useQuery } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";
// 


  
export default function ScannerDashboard() {
  const history = useHistory();
  const user = useAppSelector((state: RootState) => state.auth);
  let validRole = false;
  if (user &&  user.user?.role && user.user.role != 'Attendee')
    validRole = true;

  console.log('can scan tickets', validRole);

  const customerId = user.user?.customerId;
 
 
  const event = useAppSelector((state: RootState) => state.event);
  const { data, isLoading } = 
  useQuery(['EventsForScanner',customerId], async () => {
      console.log("Fetching events for customer id", customerId);
      try {
        const res = await axiosClient.get(`/Events/ForScanning/${customerId}`);
        console.log('events for customer with status',res?.data, res?.status);
        return res?.data;
      } catch (error) {
        console.error('Error fetching events for customer', error);
        return [];
      }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!customerId //  only run query if we have an id
    }
  );


    const ionRouter = useIonRouter();

  if (isLoading) return <p>Loading...</p>;


  return (
     <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
      <div className="max-w-md mx-auto mt-6">
        {!validRole ? (
              <h2 className="text-xl font-semibold mb-4">You do not have rights to scan tickets. 
              Contact admin/owner.</h2>
        ):(
          <>
            <h2 className="text-xl font-semibold mb-4 text-center">Select event to scan</h2>
            <div className="divide-y">
              {data && data.map((event:EventHeader) => (
                <div
                    key={event.eventId}
                    onClick={() => ionRouter.push(`/scanticket/${event.eventId}?eventName=${event.eventName}`)}
                    className="border border-gray-200 rounded-lg  cursor-pointer p-4 flex items-center justify-between hover:bg-gray-50"
                  >
                  
                  <div>
                    <p className="font-heading text-accent-color">{event.eventName}</p>
                    <div className="text-primary-color text-lg">
                      <div>{new Date(event.eventDate).toLocaleDateString()}</div>
                      <div>{event.eventLocation}</div>
                    </div>
                  </div>
                </div>  
               
              ))}    
            </div>
        </>
        )}
      </div>      
  </IonContent>
  </IonPage>
  );
}
