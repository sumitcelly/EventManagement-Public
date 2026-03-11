import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect, useRef } from "react";
import { useMutation } from "react-query";
import toast, { Toaster } from 'react-hot-toast';
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";


  
export default function EventPublish({eventId}: {eventId?:string}) {
  const history = useHistory();

  const queryClient = useQueryClient();
  const refundModeRef = useRef<HTMLSelectElement>(null);
  const ticketDisplayModeRef = useRef<HTMLSelectElement>(null);

  const { data, isLoading:validateLoading } = useQuery(['settings',eventId], async () => {
    const res = await axiosClient.get(`/events/settings/${eventId}`);
    console.log('Event settings details from backend', res?.data);
    
    return res.data;
  },
    //return {"valid":true,"publishStatus":"Draft","eventStatus":false,"ticketStatus":false, eventUrl:"https://ticketsnow.com/foodfest26"};
    {
      staleTime: 1000 * 60 * 5,
      enabled: !!eventId
    }
  );



  type PublishEventParams = {
    status: boolean;
    eventId?: string;
    refundMode?:string;
    ticketFeeMode?: string;
  };

  
  const publishEvent = async ({ status, eventId ,refundMode, ticketFeeMode}: PublishEventParams) => {
    try {
      console.log("Publishing event", eventId, "set live status to", status);

      const res = await axiosClient.put(`/events/eventsettings/${eventId}`, {
        isLive: status,
        refundMode: Number(refundMode) || 0,
        ticketFeeMode: Number(ticketFeeMode) || 0
      }, {
                  headers: {
                  'Content-Type': 'application/json'}
                });
            
      console.log("Event publish API response:", res?.data);
      const eventStatus = status?"Live":"Draft";

      if (res?.data) {
        await queryClient.resetQueries({ queryKey: ["settings", eventId] });
        console.log("✅ Success publishing event", eventId);
        toast.success("Event status changed successfully to "+eventStatus);
        return true;
      } else {
        console.warn("⚠️ There was an issue publishing your event", eventId);
         toast.error("Event failed to change event status. Please try again!");
        return false;
      }
    } catch (error) {
      console.error("❌ Error publishing event", error);
      return false;
    }
  };

  const mutation = useMutation<boolean, Error, PublishEventParams>({
    mutationFn: publishEvent,
  });

  const { mutate, isLoading, isSuccess, isError } = mutation;

  useEffect(()=>{
    if (data && refundModeRef.current && ticketDisplayModeRef.current)
    {
      refundModeRef.current.value = data.refundMode;
      ticketDisplayModeRef.current.value = data.ticketFeeMode;
    }
  },[data]);

  if (validateLoading) return <p>Loading...</p>;
  
 



  return (  
     
    <div className="max-w-md mx-auto  text-center">
      {/* <h2 className="text-2xl font-semibold mb-4 text-accent-color font-accent">Go Live!</h2> */}
      <div className="flex flex-col">
         {/* <Toaster position="top-right" /> */}
         <label className="block font-semibold italic font-accent text-accent-color">Your event is in {data?.isLive?'Live':'Draft'} status</label>
         {data && !data.isLive?(
          <div className="bg-brand-neutral mt-6 p-3 text-center text-secondary-color rounded">
       
            {
              data && data.ticketStatus && (
              <p className="text-l">
                You are ready to go online! All event and ticket details are complete!
              </p>
            )}
             {
              data && !data.ticketStatus && (
              <p className=" text-l">
               Please add atleast one ticket type.
              </p>
            )}
          </div>
          ):
            <div className="bg-brand-neutral mt-6 rounded p-2 text-center">
              <p className="text-secondary-color text-l text-center">
                  You are already online. Any changes you make to your events will be available instantly!
                </p>
            </div>
        }

        {data && data.isLive && (
          <div className= "mt-3 bg-brand-neutral rounded">
            Your event url is <a href={`${window.location.origin}/${data.eventUrlName}`}>{`${window.location.origin}/${data.eventUrlName}`}</a>
          </div>
        )}
        
        <div className="flex flex-col space-y-1 mt-4">
         <label className="block font-semibold mb-1 mr-auto">Refund Mode</label>
           <select ref={refundModeRef} className="w-1/2">
              <option value="0">No refunds allowed</option>
              <option value="1">Customer initiates refunds</option>
            </select>
        </div>

         <div className="flex flex-col space-y-1 mt-4">
          <label className="block font-semibold mb-1 mr-auto">Fee Display Mode</label>
           <select ref={ticketDisplayModeRef} className="w-1/2">
              <option value="0">None(Note required for free tickets)</option>
              <option value="1">Customer absorbs all fees</option>
              <option value="2">Organizer absorbs Stripe fees</option>
            </select>
        </div>
        
      </div>
    
    {data && data.ticketStatus && (
      <div className="flex flex-row mt-4">
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
               onClick={() => mutate({ status: !data.isLive, eventId: eventId, 
                      refundMode:refundModeRef.current?.value,
                      ticketFeeMode: ticketDisplayModeRef.current?.value })}
                disabled={mutation.isLoading}
              >
               {!data.isLive? "Publish" :  "Unpublish"}
          </button> 
          
         
      </div>
    )}

  </div>
 
  );
}
