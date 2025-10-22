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
// 


  
export default function EventPublish({eventId}: {eventId?:string}) {
  const navigate = useNavigate();
  const user = useAppSelector((state: RootState) => state.auth);
  const userId = user.user?.id;

  const { data, isLoading } = useQuery(['validate',eventId], async () => {
    // const res = await axiosClient.get(`/events/validate/${eventId}`);
    // console.log('Event validation details from backend', res?.data);
    // return res.data;
    return {"valid":false,"publishStatus":"Draft","eventStatus":false,"ticketStatus":false, eventUrl:""};
    {
      //staleTime: 1000 * 60 * 5,
      enabled: !!eventId
    }
  });

  if (isLoading) return <p>Loading...</p>;
  
  const mutation = useMutation({
    mutationFn: () => publishEvent(eventId),
  });

  const publishEvent= async(eventId?:string)=>
  {
    try{
      console.log("event publish starting for eventid",eventId);
      const res ={data:{"status":true}};
      // const res = await axiosClient.post(`/events/publish/${eventId}`);
      console.log('Event publish status', res?.data);
      if (res?.data.status)
      {
        return "Event is online!"
      }
      else
      {
        return "There was an issue publishing your event. Please try later!"
      }
    }
    catch(error)
    {
      console.log("error publishing event", error);
       return "There was an issue publishing your event. Please try later!"
    }
    

  }
  
  return (
    <div className="max-w-md mx-auto mt-6">
      <h2 className="text-xl font-semibold mb-4">Go Live!</h2>
      <div className="flex flex-col">
       
         <label className="block font-semibold mt-5">Event Status: {data?.eventStatus}</label>
         {data && data.publishStatus === "Draft"?(
          <div className="bg-brand-neutral mt-6">
            {
              data && data.valid && (
              <p className="text-go-color text-xl">
                You are ready to go online! All event and ticket details are compelte!
              </p>
            )}
             {
              data && !data.valid && !data.eventStatus && (
              <p className="text-secondary-color text-xl">
               Event details are missing. Please complete those before proceeding.
              </p>
            )}
             {
              data && !data.valid && !data.ticketStatus && (
              <p className="text-secondary-color text-xl">
              You must save at least one ticket for your event before proceeding.
              </p>
            )}
          </div>
          ):
            <div className="bg-brand-neutral mt-6">
              <p className="text-go-color text-xl">
                  You are already online! Any changes you make to your events will be available instantly!
                </p>
            </div>
        }

        {data && data.valid && (
          <div className="bg-brand-dark">
            Your event url is {data?.eventUrl}
          </div>
        )}
        
      </div>
    
    {data && data.publishStatus==="Draft" && data.valid && (
      <div className="flex flex-row mt-4">
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                onClick={() => mutation.mutate()}
                disabled={mutation.isLoading}
              >
               {mutation.isLoading ? "Publishing..." : "Publish"}
          </button> 
          
          {mutation.isSuccess && (
            <p className="text-go-color text-sm">Event published successfully!</p>
          )}
          {mutation.isError && (
            <p className="text-secondary-color text-sm">Failed to publish event.</p>
          )}
      </div>
    )}

  </div>
  );
}
