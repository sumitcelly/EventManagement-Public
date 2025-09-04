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
        eventDate: "9/15/2025",
        eventTime: "18:00",
      eventSummary: "Taste dishes from top chefs and local favorites.   This is a great opportunity to bring your pets and your kids for a fun day at the park. come one, come all. Enjoy the rides. Lots of free music and food.",
      eventId: 1, eventName: "Food Festival 2025",
      eventHeadliner: "Food, fund at the park.  Taste dishes from top chefs and local favorites. And more...",
      eventDescription: "Taste dishes from top chefs and local favorites. And more\n And there is more and more \n And even more \n And still more \n And finally the last bit of more.",
       eventOrganizer: "Polka dots and curry", eventLocation: "121 Central Park, New York, NY 10001",
       evetOrganizerPictureUrl: "/images/concert2.jpg",
        eventOrganinizerInstagramUrl:  "https://www.instagram.com/polkadotsandcurry/",
        eventOrganinizerTwitterUrl:  "https://twitter.com/polkadotsandcurry",
        eventOrganinizerFacebookUrl:  "https://www.facebook.com/polkadotsandcurry",
       eventOrganizerDescription: "Polka dots and curry is a fun event organizer that loves to bring fun events to the city. We specialize in food, music and art events. Our mission is to bring joy and happiness to everyone through our events.",
        eventAgenda: "- 6:00 PM: Gates Open\n- 7:00 PM: Opening Act\n- 8:00 PM: Headliner Performance\n- 10:00 PM: Event Close"
     
      }
  return (
 
    <div className= "max-w-2xl mx-auto mt-3 flex-col border border-gray-300 rounded-lg p-6 shadow-lg bg-brand-neutral">
      <div className="text-3xl text-center text-primary-color font-heading font-bold">{testEvent.eventName}</div>
      <div className="text-l text-center font-body mt-3 text-secondary-color">{testEvent.eventHeadliner}</div>
      <div className="flex flex-row mt-4 bg-">
          <img src="/images/concert.jpg"  alt={testEvent.eventName} 
            className="rounded-lg shadow-md w-2/3" />
          <div className="flex flex-col justify-center ml-4">
    
         
            {testEvent.isFree &&
              <div className="text-center text-tertiary-color">Free Event!</div>
              }
            {!testEvent.isFree &&
              <div className="text-center text-tertiary-color">Tickets from $20</div>
              }
            <Button
              className="align-bottom mt-auto align-center ml-4"
                    size="xs"
                    onClick={() => navigate(`/event/${testEvent.eventId}`)}>
                    Get your Tickets
            </Button>
          </div>
      </div>
      <div className="flex italic font-body mt-4 font-extrabold">
        <div className="text-l font-headline text-primary-color">
          {new Date(testEvent.eventDate).toLocaleDateString()} {testEvent.eventTime} 
        </div>

        <div className="font-headline text-primary-color ml-auto">
          {testEvent.eventLocation} 
        </div>
    </div>
      <div className="text-sm font-body mt-3 text-primary-color 
                    border rounded-lg p-2 shadow-lg bg-brand-neutrallight">
        {testEvent.eventSummary}
      </div>
      <div className="text-sm font-body mt-3 text-primary-color 
                border rounded-lg p-2 shadow-lg">
          <div className="text-lg font-bold mb-2 text-center text-primary-color">Event Agenda</div>
          {testEvent.eventAgenda.split('\n').map((line, index) => (
            <li key={index}>{line}</li>
          ))}
      </div>
      <div className=" bg-brand-neutrallight text-sm font-body mt-3 text-primary-color 
                border rounded-lg p-2 shadow-lg">
          <div className="text-lg font-bold mb-2 text-center text-primary-color">More Info ...</div>
          {testEvent.eventDescription.split('\n').map((line, index) => (
            <p key={index} className="mb-2">{line}</p>
          ))}
      </div>

      <div className="flex flex-row mt-4 items-center
                border rounded-lg p-2 shadow-lg">
          <img src={testEvent.evetOrganizerPictureUrl}  alt={testEvent.eventOrganizer}  
            className="rounded-full shadow-md w-24 h-24" />
          <div className="flex flex-col justify-center ml-4">
            <div className="text-l font-bold text-primary-color">{testEvent.eventOrganizer}</div>
            <div className="text-sm font-body text-primary-color">{testEvent.eventOrganizerDescription}</div>
            <div className="flex flex-row mt-2 space-x-4">
              <Link to={testEvent.eventOrganinizerInstagramUrl} target="_blank" className="text-pink-500 hover:underline">Instagram</Link>
              <Link to={testEvent.eventOrganinizerTwitterUrl} target="_blank" className="text-blue-400 hover:underline">Twitter</Link>
              <Link to={testEvent.eventOrganinizerFacebookUrl} target="_blank" className="text-blue-600 hover:underline">Facebook</Link>
            </div>
          </div>
      </div>
    </div>
  
   
  );
}
