import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { IonHeader, useIonRouter } from "@ionic/react";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { IonContent, IonPage, IonRouterLink } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import { useHistory } from "react-router";
import Footer from "../components/Footer";
// 
interface UserSalesOrder {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventSummary: string;
  eventOrganizer: number;
  eventLocation: string;
  salesOrderCode: string;
  salesOrderStatus:String;
  salesOrderId:number;
  eventHeadline:string;
  salesOrderTotal: number;
  totalFees: number;
  platformFees: number;
  salesTax: number;
  organizerUrlName: string;
  eventUrlName: string;
}


  
export default function MyEvents() {
  //const navigate = useNavigate();
  const router = useIonRouter();
  const history = useHistory();
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
      <IonContent className="">
      <div className="flex flex-col min-h-full">
     
        <div className="max-w-md mx-auto mt-6">
        <h2 className="text-xl font-semibold mb-4">My Upcoming Events</h2>
        <ListGroup>
          {data && data.map((event:UserSalesOrder) => (
              <ListGroupItem
              key={event.salesOrderCode}
              onClick={(e) =>{ 
                console.log("Navigating to event details for eventId:", event.eventId);
                history.push(`/eventdetails/${event.organizerUrlName}/${event.eventUrlName}`,'forward'); }}
              className="cursor-pointer  bg-brand-panelbg"
            >
            <div className="flex items-center justify-between gap-4 w-full">
              <div className="min-w-0">
                <p className="font-heading text-accent-color">{event.eventName}</p>
                <p className="text-font-heading text-primary-color text-lg">
                  {new Date(event.eventDate).toLocaleDateString()} · {event.eventLocation}
                </p>
                <p className="text-sm text-accent-color  smt-1">{event.eventHeadline}</p>
              </div>
              {/*Do  not use <a> </a> tag. since that creates a full load and react query's keys get reset}*/}
              {/* <IonRouterLink
                routerLink={`/ticketdetails/${event.eventId}/${event.salesOrderCode}/${event.salesOrderId}/${event.salesOrderStatus}`}
                
                onClick={(e) => e.stopPropagation()}
                className="ml-4 px-3 py-1 text-sm font-body text-white bg-brand-light rounded inline-flex"
              >
                View tickets
              </IonRouterLink> */}
              <div className="flex flex-col">
              <div
                onClick={(e) => {
                  e.stopPropagation();
                  history.push(`/ticketdetails`, {
                    eventId: event.eventId,
                    salesOrderCode: event.salesOrderCode,
                    salesOrderId: event.salesOrderId,
                    salesOrderStatus: event.salesOrderStatus,
                    salesOrderTotal : event.salesOrderTotal || 0,
                    totalFees : event.totalFees || 0,
                    platformFees : event.platformFees || 0,
                    salesTax: event.salesTax || 0
                  });
                }}
                className="ml-4 px-3 py-1 text-sm font-body text-white bg-brand-light rounded inline-flex cursor-pointer"
              >
                View tickets
              </div>
              <p className="mt-1 ml-auto text-center text-xxs text-tertiary-color">{event.salesOrderCode}</p>
              </div>
            
              
          </div>
            </ListGroupItem>
        
          ))}
                        

      </ListGroup>
    
      </div>
      <Footer/>
    </div>
    
    </IonContent>
    
    </IonPage>
  );
}
