import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { IonHeader, useIonRouter } from "@ionic/react";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { IonContent, IonPage, IonRouterLink } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
// 
interface UserSalesOrder {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventSummary: string;
  eventOrganizer: number;
  eventLocation: string;
  salesOrderCode: string;
  eventHeadline:string;
}


  
export default function MyEvents() {
  //const navigate = useNavigate();
  const router = useIonRouter();
  const  user = useAppSelector((state:RootState) => state.auth);
  const userId= user.user?.id;

  const { data, isLoading } = 
  useQuery(['SalesOrderByUserId',userId], async () => {
     console.log("Fetching orders for user", userId);
      const res = await axiosClient.get(`/SalesOrder/ByUserId/${userId}`);
      console.log('orders fetched from backend',res.data);
      return res.data;
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!userId //  only run query if we have an id
    }
  );

  if (isLoading) return <p>Loading...</p>;

  return (
     <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent  className="ion-padding flex flex-col justify-center items-center h-full">
     
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">My Upcoming Events</h2>
      <ListGroup>
        {data && data.map((event:UserSalesOrder) => (
            <ListGroupItem
            key={event.salesOrderCode}
            onClick={(e) =>{ 
              console.log("Navigating to event details for eventId:", event.eventId);
              router.push(`/eventdetails/${event.eventId}`,'forward'); }}
            className="cursor-pointer"
          >
          <div className="flex items-center justify-between gap-4 w-full">
            <div className="min-w-0">
              <p className="font-heading text-accent-color">{event.eventName}</p>
              <p className="text-font-heading text-primary-color text-lg">
                {new Date(event.eventDate).toLocaleDateString()} · {event.eventLocation}
              </p>
              <p className="text-sm text-font-body mt-1">{event.eventHeadline}</p>
            </div>
            {/*Do  not use <a> </a> tag. since that creates a full load and react query's keys get reset}*/}
            {/* <IonRouterLink
              routerLink={`/ticketdetails/${event.eventId}/${event.salesOrderCode}`}
              onClick={(e) => e.stopPropagation()}
              className="ml-4 px-3 py-1 text-sm font-body text-white bg-brand-light rounded inline-flex"
            >
              View tickets
            </IonRouterLink> */}
            <Button
              onClick={(e) => {
                e.stopPropagation();
                router.push(`/ticketdetails/${event.eventId}/${event.salesOrderCode}`, 'forward');
              }
              }
              className="ml-4 px-3 py-1 text-sm font-body text-white bg-brand-light rounded inline-flex"
            >
              View Tickets
            </Button>
            
        </div>
          </ListGroupItem>
      
        ))}
                      

    </ListGroup>
    </div>
    </IonContent>
    </IonPage>
  );
}
