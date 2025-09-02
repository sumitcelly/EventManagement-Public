import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useLocation } from "react-router-dom";


export default function TicketDetails() {
  // const { data, isLoading } = useQuery("ticketdetails", async () => {
  //   const res = await axiosClient.get("/ticketdetails",{userId: "currentUserId",eventId: "hh"});
  //   return res.data;
  // });

  //if (isLoading) return <p>Loading...</p>;
    const location  = useLocation();
    //const {id}  = useParams();
    const { id } = location.state || {} ;
    //alert(eventName);
  return (
 
    <div className="p-4">
      <h2 className="text-xl font-bold mb-2">Ticket Details for event {id}</h2>
      
    </div>
  );
}
