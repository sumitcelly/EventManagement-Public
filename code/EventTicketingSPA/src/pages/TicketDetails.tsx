import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useLocation, useParams } from "react-router-dom";
import { EventTickets } from "../types/Tickets";
import EventSummary from "../components/EventSummary";
import { EventHeader } from "../types/Event";
import { useState } from "react";
//import AppPagination from "../components/Pagination";
import SalesOrderTicket from "../components/SalesOrderTicket";
import App from "../App";
import AppPagination from "../components/Pagination";

export default function TicketDetails() {
  
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const onPageChange = (page: number)=>{
        console.log("onpagechange",page);
        setCurrentPage(page);
  }

  const { eventId } = useParams();
   const  sampleTickets: EventTickets[] = [
    { id: 1, name: "General Admission", qrCode: "QR123456" },
    { id: 2, name: "VIP Pass", qrCode: "QR654321"},
    { id: 3, name: "Balcony Seat", qrCode: "QR789012" },
  ];

  const { data, isLoading } = useQuery("ticketdetails", async () => {
    //const res = await axiosClient.get("/ticketdetails",{userId: "currentUserId",eventId: "hh"});
    if (currentPage === 1)
        setTotalItems(sampleTickets.length);
  
    return sampleTickets;
    });


  if (isLoading) return <p>Loading...</p>;
 
 
 const sampleEvent  = {
    eventId:   parseInt(eventId || "1"),
    eventName: "Sample Event",
    eventDate: new Date(),
    eventLocation: "Sample Location",
  };

  return (
 
      <div className="flex flex-col max-w-md mx-auto ">
        
          <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center">Ticket Details</div>       
         
          {/* <EventSummary eventBasic={sampleEvent}/>  */}
      
          {data && <SalesOrderTicket eventBasic={sampleEvent} 
              tickets={[{id: data[currentPage-1].id, name:data[currentPage-1].name, 
                description:"", cost:0, quantity:1}]} 
              salesOrderCode={data[currentPage-1].qrCode} qrBase64String={""}>
            
            </SalesOrderTicket>
          }
                
         <div className="ml-auto mb-4">
            <AppPagination totalItems={totalItems} currentPage={currentPage} onPageChange={onPageChange} itemsPerPage={1}></AppPagination>
         </div>
          
    </div>
  );
}
