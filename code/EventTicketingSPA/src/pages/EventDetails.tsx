import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useParams } from "react-router-dom";
import { Button} from "flowbite-react";
import { useNavigate,Link } from "react-router-dom";
interface Event {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventDescription: string;
  eventOrganizer: number;
  eventLocation: string;
  eventHeadliner: string;
  isFree: boolean
}

export default function EventDetails() {
    const navigate = useNavigate();
  // const { data, isLoading } = useQuery("ticketdetails", async () => {
  //   const res = await axiosClient.get("/ticketdetails",{userId: "currentUserId",eventId: "hh"});
  //   return res.data;
  // });

  //if (isLoading) return <p>Loading...</p>;
    //const location  = useLocation();
    const {id}  = useParams();
    //const { id } = location.state || {} ;
    //alert(eventName);

      const  testEvent = {
        isFree: false,
      eventSummary: "Taste dishes from top chefs and local favorites.   This is a great opportunity to bring your pets and your kids for a fun day at the park. come one, come all. Enjoy the rides. Lots of free music and food.",
      eventId: 1, eventName: "Food Festival 2025", eventDate: new Date(), 
      eventHeadliner: "Food, fund at the park.  Taste dishes from top chefs and local favorites. And more...",
      eventDescription: "Taste dishes from top chefs and local favorites. And more...",
       eventOrganizer: 4, eventLocation: "New York"
    }
  return (
 
    <div className= "max-w-2xl mx-auto mt-3 flex-col border border-gray-300 rounded-lg p-6 shadow-lg">
      <div className="text-2xl text-center text-primary-color font-heading">{testEvent.eventName}</div>
      <div className="text-l text-center font-body mt-3">{testEvent.eventHeadliner}</div>
      <div className="flex flex-row mt-4">
          <img src="/images/concert.jpg"  alt={testEvent.eventName} 
            className="w-2/3  rounded-lg shadow-md" />
          <div className="flex flex-col w-1/3">
            {testEvent.isFree &&
              <div className="align-top text-center text-tertiary-color">Free Event!</div>
              }
            {!testEvent.isFree &&
              <div className="align-top text-center text-secondary-color">Tickets from $20</div>
              }
            <Button
              className="align-bottom mt-auto align-center ml-4"
                    size="xs"
                    onClick={() => navigate(`/event/${testEvent.eventId}`)}>
                    Buy Tickets
            </Button>
          </div>
      </div>
        <div className="text-sm font-body mt-5 text-primary -color 
                      border bg-brand-light rounded-lg p-2 shadow-lg">
          {testEvent.eventSummary}</div>

    </div>
  
   
  );
}
