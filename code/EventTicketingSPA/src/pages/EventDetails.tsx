import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useLocation, useParams } from "react-router-dom";
import { Button } from "flowbite-react";
import { useHistory, Link } from "react-router-dom";
import { EventHeader} from "../types/Event";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import  { updateEvent} from "../features/auth/eventSlice";
import { resetCart } from "../features/auth/cartSlice";
import { useAppDispatch } from "../app/hook";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import Footer from "../components/Footer";

export default function EventDetails() {

  const { customerName, eventName } = useParams<{ customerName: string,eventName:string }>();
  const ionRouter = useIonRouter();
  const dispatch = useAppDispatch();
  const location = useLocation();
  const {mode}  = location.state as any || {};
  const user = useAppSelector((state:RootState) => state.auth.user);
 

  const { data:eventDetails, isLoading } = useQuery(`events/details/${customerName}/${eventName}`, async () => {
    if (mode && mode === "preview")
    {
       const res = await axiosClient.get(`/events/detailspreview/${user?.customerId}/${customerName}/${eventName}`);
       console.log('Preview Event details from backend', res?.data);
       return res.data;
    }
    else
    {
      const res = await axiosClient.get(`/events/details/${customerName}/${eventName}`);
      console.log('Event details from backend', res?.data);
      return res.data;
    }
  },
  {
    staleTime: 1000 * 60 * 5,
    enabled: !!customerName && !!eventName
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
      organizerStripeAccountId: organizerDetails?.stripeAccountId,
      refundMode: eventDetails.refundMode,
      ticketFeeMode: eventDetails.ticketFeeMode
    }

    if (!event)
      return;
    
    dispatch(updateEvent({event}));
    //need to think if this needs to be done after checking if the event id is different than the above?
    dispatch(resetCart());
    ionRouter.push(`/buytickets/${eventDetails.eventId}`);
  }
  
  if (isLoading) return <p>Loading...</p>;

  if (!eventDetails)
  {
  return <p>Unable to locate event</p>
  }
  
  const handleCopy = async (e:any) => {
    console.log('copy text',e);
  try {
    await navigator.clipboard.writeText(e.target.innerText);
    
  } catch (err) {
    console.error("Failed to copy: ", err);
  }
};

  return (
    <IonPage>
      <IonHeader><AppNavbar/></IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
        
    <div className= "max-w-2xl mx-auto mt-3 flex-col border border-gray-300 rounded-lg p-6 shadow-lg bg-brand-neutral">
      <div className="text-3xl text-center text-primary-color font-heading font-bold">{eventDetails.eventName}</div>
      <div className="text-l text-center font-body mt-3 text-secondary-color">{eventDetails.eventHeadline}</div>
      <div className="flex flex-col mt-4 gap-4"> 
        {/* Header Row: Button on the far right */}
        <div className="flex justify-between items-center w-full px-1">
          <div>
            {eventDetails.isFree && (
              <span className="text-tertiary-color font-bold text-sm">
                Free Event!
              </span> 
            )}
          </div>
          
          <Button size="xs" onClick={() => handleGetTickets()}> 
            Get your Tickets 
          </Button> 
        </div>

        {/* Image: Now full width below the button */}
        <img 
          src={eventDetails.eventBannerUrl} 
          alt={eventDetails.eventName} 
          className="rounded-lg shadow-md w-full max-h-[400px] object-contain bg-brand-panelbg" 
        /> 
      </div>

      <div className="flex flex-row justify-center  italic font-body mt-4 font-extrabold">
        <div className="text-l font-headline text-primary-color">
          {new Date(eventDetails.eventDate + 'Z').toLocaleString()} 
        </div>

        <div className="font-headline text-primary-color ml-auto w-1/2 hover:bg-gray-100  cursor-pointer"
            onClick={handleCopy} 
            title ="Click to copy">
          {eventDetails.eventLocation} 
        </div>
    </div>

    {eventDetails.eventAgenda && 
    (
      <div className="text-sm font-body mt-3 text-primary-color 
                border rounded-lg p-2 shadow-lg bg-brand-panelbg">
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
              border rounded-lg p-2 shadow-lg bg-brand-panelbg">
        <div className="text-lg font-bold mb-1 text-center text-primary-color">More Info ...</div>
        <div className="ml-5" dangerouslySetInnerHTML={{ __html: eventDetails.eventDescription }} />
        {/* {eventDetails.eventDescription.split('\n').map((line:string, index:number) => (
          <p key={index} className="mb-2">{line}</p>
        ))} */}
    </div>
  )}

    {!isOrganizerLoading && (
      <div className="flex flex-row mt-4 items-center
                border rounded-lg p-2 shadow-lg bg-brand-panelbg">

          {organizerDetails?.organizerWebsite ?(
            <a href={organizerDetails.organizerWebsite} target="_blank" rel="noreferrer" className="text-blue-500 hover:underline mb-2">
              <img
                src={organizerDetails.organizerImageUrl}
                alt={organizerDetails.organizationName}
                className="rounded-full shadow-md w-24 h-24"
              />
            </a>
          ) : (
            <img
              src={organizerDetails?.organizerImageUrl}
              alt={organizerDetails?.organizationName}
              className="rounded-full shadow-md w-24 h-24"
            />
          )}
          
          <div className="flex flex-col justify-center ml-4">
            <div className="text-l font-bold text-primary-color">{organizerDetails.organizationName}</div>
            <div className="text-sm font-body text-primary-color">{organizerDetails.organizerAboutMe}</div>
            <div className="flex flex-row mt-2 space-x-4">
              <a href={`mailto:${organizerDetails.organizerEmail}`} className="text-pink-500 hover:underline">Email</a>
              {/* {organizerDetails.organizerPhone && <a href={`tel:${organizerDetails.organizerPhone}`} className="text-green-500 hover:underline">Phone</a>} */}
              {organizerDetails.organizerInstagram && <a href={organizerDetails.organizerInstagram} target="_blank" className="text-pink-500 hover:underline">Instagram</a>}
              {organizerDetails.organizerX && <a href={organizerDetails.organizerX} target="_blank" className="text-blue-400 hover:underline">X</a>}
              {organizerDetails.organizerFacebook && <a href={organizerDetails.organizerFacebook} target="_blank" className="text-blue-600 hover:underline">Facebook</a>}
            
            </div>
          </div>
      </div>
    )}
    
    </div>
    <Footer/>
    </IonContent>
    </IonPage>
  

   
  );
}
