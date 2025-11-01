import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect } from "react";
import { useMutation } from "react-query";
import toast, { Toaster } from 'react-hot-toast';


  
export default function EventPublish({eventId}: {eventId?:string}) {
  const navigate = useNavigate();

  const queryClient = useQueryClient();

  const { data, isLoading:validateLoading } = useQuery(['validate',eventId], async () => {
    const res = await axiosClient.get(`/events/livestatus/${eventId}`);
    console.log('Event validation details from backend', res?.data);
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
  };

  
  const publishEvent = async ({ status, eventId }: PublishEventParams) => {
    try {
      console.log("Publishing event", eventId, "set live status to", status);

      const res = await axiosClient.put(`/events/livestatus/${eventId}`, status, {
                  headers: {
                  'Content-Type': 'application/json'}
                });
            
      console.log("Event publish API response:", res?.data);
      const eventStatus = status?"Live":"Draft";

      if (res?.data) {
        await queryClient.resetQueries({ queryKey: ["validate", eventId] });
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


  if (validateLoading) return <p>Loading...</p>;
  
  return (  
    <div className="max-w-md mx-auto  text-center">
      {/* <h2 className="text-2xl font-semibold mb-4 text-accent-color font-accent">Go Live!</h2> */}
      <div className="flex flex-col">
         <Toaster position="top-right" />
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
            Your event url is <a href={`${window.location.origin}/${data.sanitizedEventName}`}>{`${window.location.origin}/${data.sanitizedEventName}`}</a>
          </div>
        )}
        
      </div>
    
    {data && data.ticketStatus && (
      <div className="flex flex-row mt-4">
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
               onClick={() => mutate({ status: !data.isLive, eventId: eventId })}
                disabled={mutation.isLoading}
              >
               {!data.isLive? "Publish" :  "Unpublish"}
          </button> 
          
         
      </div>
    )}

  </div>
  );
}
