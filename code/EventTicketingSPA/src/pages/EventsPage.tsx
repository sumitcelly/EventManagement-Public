import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
// 
interface UserSalesOrder {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventDescription: string;
  eventOrganizer: number;
  eventLocation: string;
  salesOrderCode: string;
}


  
export default function EventsPage() {
  const navigate = useNavigate();
  const  user = useAppSelector((state:RootState) => state.auth);
  const userId= user.user?.id;

  const { data, isLoading } = 
  useQuery( ['eventsByUserId', userId], async () => {
     console.log("Fetching orders for user", user.user?.id);
      const res = await axiosClient.get(`/SalesOrder/ByUserId/${userId}`);
      console.log('orders fetched from backebend',res.data);
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
   console.log("Fetching orders for user", user.user?.id);
  return (
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">My Upcoming Events</h2>
      <ListGroup>
        {data && data.map((event:UserSalesOrder) => (
            <ListGroupItem
            key={event.salesOrderCode}
            onClick={() => navigate(`/eventdetails/${event.eventId}`)}
            className="cursor-pointer"
          >
            <div className="grid grid-cols-[1fr_auto] gap-4 items-center">
                <div>
                  <p className="font-heading text-accent-color">{event.eventName}  </p>
                 
                  <p className="text-font-heading text-primary-color text-lg">
                    {new Date(event.eventDate).toLocaleDateString()} · {event.eventLocation}
                  </p>
                  <p className="text-sm text-font-body mt-1">{event.eventDescription} </p> 
                </div>

                  <a
                     href={`/ticketdetails/${event.eventId}/${event.salesOrderCode}`}
                     onClick={(e) => {
                      e.stopPropagation(); // Prevent the ListGroupItem onClick from firing
                    }}
                    className="px-3 py-1 text-sm font-body text-white bg-brand-light rounded"
                  >
                    View tickets
                  </a>
             

            </div>
          </ListGroupItem>
      
        ))}
                      

    </ListGroup>
    </div>
  );
}
