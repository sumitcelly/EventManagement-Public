import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
// 
interface Event {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventDescription: string;
  eventOrganizer: number;
  eventLocation: string;
}


   
export default function EventsPage() {
  const navigate = useNavigate();
  const { data, isLoading } = useQuery("eventsbyname", async () => {
  const res = await axiosClient.get("/events/byname/new7");


  const events: Event[] = [];
  if (res.data) {
    
    events.push({ eventId: 1, eventName: "Food Festival", eventDate: new Date(), eventDescription: "Taste dishes from top chefs and local favorites. And more...", eventOrganizer: 4, eventLocation: "New York" });
    events.push({ eventId: 2, eventName: "Music festival", eventDate: new Date(), eventDescription: "Explore contemporary music from around the world.", eventOrganizer: 2, eventLocation: "New York" });
    events.push({ eventId: 3, eventName: "Art Exhibition", eventDate: new Date(), eventDescription: "Explore contemporary artworks from around the world.", eventOrganizer: 2, eventLocation: "New York" });
    events.push({ eventId: 4, eventName: "Tech Conference", eventDate: new Date(), eventDescription: "Join industry leaders to discuss the latest in technology.", eventOrganizer: 3, eventLocation: "New York" });
  }

  return events;
});

  if (isLoading) return <p>Loading...</p>;

  return (
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">My Upcoming Events</h2>
      <ListGroup>
        {data && data.map((event:Event) => (
            <ListGroupItem
            key={event.eventId}
            onClick={() => navigate(`/eventdetails/${event.eventId}`)}
            className="cursor-pointer"
          >
            <div className="grid grid-cols-[1fr_auto] gap-4 items-center">
                <div>
                  <p className="font-heading text-accent">{event.eventName}  </p>
                 
                  <p className="text-font-heading text-primary-text text-lg">
                    {new Date(event.eventDate).toLocaleDateString()} · {event.eventLocation}
                  </p>
                  <p className="text-sm text-font-body mt-1">{event.eventDescription} </p> 
                </div>
              
                <Button
                  size="xs"
                  onClick={() => navigate(`/tickets/${event.eventId}`)}>
                  View Tickets
                </Button>

            </div>
          </ListGroupItem>
      
        ))}
                      

    </ListGroup>
    </div>
  );
}
