import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useParams } from "react-router-dom";
import { Button } from "flowbite-react";
import { useHistory, Link } from "react-router-dom";
import { EventHeader} from "../types/Event";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import  { updateEvent} from "../features/auth/eventSlice";
import { resetCart } from "../features/auth/cartSlice";
import { useAppDispatch } from "../app/hook";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
import Navbar from "../components/Navbar";

export default function EventDetails() {

  const { id } = useParams<{ id: string }>();
  const ionRouter = useIonRouter();
  const dispatch = useAppDispatch();

  const { data:eventDetails, isLoading } = useQuery(`events/details/${id}`, async () => {
    const res = await axiosClient.get(`/events/details/${id}`);
    console.log('Event details from backend', res?.data);
    return res.data;
  },
  {
    staleTime: 1000 * 60 * 5,
    enabled: !!id
  }
);


// Fetch organizer details using a separate useQuery
const { data: organizerDetails, isLoading: isOrganizerLoading } = useQuery(
  eventDetails?.eventOrganizerId ? `organizer/details/${eventDetails.eventOrganizerId}` : '',
  async () => {
    const res = await axiosClient.get(`/eventorganizer/${eventDetails.eventOrganizerId}`);
    console.log('Event organizer details', res?.data);
    return res.data;
  },
  {
    staleTime: 1000 * 60 * 5,
    enabled: !!eventDetails?.eventOrganizerId // Only run if eventOrganizer exists
  }
);
 

  const handleGetTickets = () => {
    const event: EventHeader = {
      eventId: eventDetails.eventId,
      eventName: eventDetails.eventName,
      eventDate: new Date(eventDetails.eventDate),
      eventLocation: eventDetails.eventLocation,
      eventOrganizerId: eventDetails.eventOrganizerId,
      organizerStripeAccountId: organizerDetails?.stripeAccountId
    }

    if (!event)
      return;

    dispatch(updateEvent({event}));
    //need to think if this needs to be done after checking if the event id is different than the above?
    dispatch(resetCart());
    ionRouter.push(`/buytickets/${id}`);
  }
  
  if (isLoading) return <p>Loading...</p>;

  return (
    <IonPage>
      <IonHeader><AppNavbar/></IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
        
    <div className= "max-w-2xl mx-auto mt-3 flex-col border border-gray-300 rounded-lg p-6 shadow-lg bg-brand-neutral">
      <div className="text-3xl text-center text-primary-color font-heading font-bold">{eventDetails.eventName}</div>
      <div className="text-l text-center font-body mt-3 text-secondary-color">{eventDetails.eventHeadline}</div>
      <div className="flex flex-row mt-4 bg-">
          <img src={eventDetails.eventBannerUrl}  alt={eventDetails.eventName} 
            className="rounded-lg shadow-md w-2/3" />
          <div className="flex flex-col justify-center ml-4">
    
         
            {eventDetails.isFree &&
              <div className="text-center text-tertiary-color">Free Event!</div>
              }
            {/* {!eventDetails.isFree &&
              <div className="text-center text-tertiary-color">Tickets from $20</div>
              } */}
            <Button
              className="align-bottom mt-auto align-center ml-4"
                    size="xs"
                    onClick={() => handleGetTickets()}>
                    Get your Tickets
            </Button>
          </div>
      </div>
      <div className="flex flex-row justify-center  italic font-body mt-4 font-extrabold">
        <div className="text-l font-headline text-primary-color">
          {new Date(eventDetails.eventDate).toLocaleString()} 
        </div>

        <div className="font-headline text-primary-color ml-auto w-1/2">
          {eventDetails.eventLocation} 
        </div>
    </div>
    
    {eventDetails.eventSummary && 
    (
      <div className="text-sm font-body mt-3 text-primary-color 
                    border rounded-lg p-2 shadow-lg bg-brand-neutrallight">
        {eventDetails.eventSummary}
      </div>
    )}

    {eventDetails.eventAgenda && 
    (
      <div className="text-sm font-body mt-3 text-primary-color 
                border rounded-lg p-2 shadow-lg">
          <div className="text-lg font-bold mb-2 text-center text-primary-color">Event Agenda</div>
          {/* {eventDetails.eventAgenda.split('\n').map((line:string, index:number) => (
            <li className="ml-5" key={index}>{line}</li>
          ))}
           */}
           <div className="ml-5" dangerouslySetInnerHTML={{ __html: eventDetails.eventAgenda }} />
      </div>
    )}

  {eventDetails.eventDescription &&( 
    <div className="bg-brand-neutrallight text-sm font-body mt-3 text-primary-color 
              border rounded-lg p-2 shadow-lg">
        <div className="text-lg font-bold mb-1 text-center text-primary-color">More Info ...</div>
        <div className="ml-5" dangerouslySetInnerHTML={{ __html: eventDetails.eventDescription }} />
        {/* {eventDetails.eventDescription.split('\n').map((line:string, index:number) => (
          <p key={index} className="mb-2">{line}</p>
        ))} */}
    </div>
  )}

    {!isOrganizerLoading && (
      <div className="flex flex-row mt-4 items-center
                border rounded-lg p-2 shadow-lg">
          <img src={organizerDetails.organizerImageUrl}  alt={organizerDetails.organizationName}  
            className="rounded-full shadow-md w-24 h-24" />
          <div className="flex flex-col justify-center ml-4">
            <div className="text-l font-bold text-primary-color">{organizerDetails.organizationName}</div>
            <div className="text-sm font-body text-primary-color">{organizerDetails.organizerAboutMe}</div>
            <div className="flex flex-row mt-2 space-x-4">
              <a href={`mailto:${organizerDetails.organizerEmail}`} className="text-pink-500 hover:underline">Email</a>
              <Link to={organizerDetails.organizerInstagram} target="_blank" className="text-pink-500 hover:underline">Instagram</Link>
              <Link to={organizerDetails.organizerX} target="_blank" className="text-blue-400 hover:underline">X</Link>
              <Link to={organizerDetails.organizerFacebook} target="_blank" className="text-blue-600 hover:underline">Facebook</Link>
            </div>
          </div>
      </div>
    )}
    
    </div>
    
    </IonContent>
    </IonPage>
  

   
  );
}
