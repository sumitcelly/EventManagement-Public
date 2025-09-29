import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useLocation, useParams } from "react-router-dom";
import { EventTickets } from "../types/Tickets";
import EventSummary from "../components/EventSummary";
import { useState } from "react";
//import AppPagination from "../components/Pagination";
import SalesOrderTicket from "../components/SalesOrderTicket";
import App from "../App";
import AppPagination from "../components/Pagination";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";

export default function TicketDetails() {
  
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const onPageChange = (page: number)=>{
        console.log("onpagechange",page);
        setCurrentPage(page);
  }

  const { eventId, salesOrderCode } = useParams();
  //const  eventDetails = useAppSelector((state:RootState) => state.event);
  
  console.log('sales order code and event id',salesOrderCode, eventId );

  const {
        data: eventDetails, // provide default empty array
        isLoading:eventLoading,
        error
  } = 
  useQuery(
    ['eventDetails', eventId], // structured query key
    async () => {
      console.log("in eventdetails backend");
      const res = await axiosClient.get(`/events/details/${eventId}`);
      console.log('Event id details', res?.data);
      return res.data;
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      //refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: !!eventId //  only run query if we have an id
    }
  );

  const { data, isLoading } = useQuery( ['ticketDetauls', eventId,salesOrderCode], async () => {
    const res = await axiosClient.get(`/Ticket/ByEventIdAndSalesOrderQrCode/${eventId}/${salesOrderCode}`);
    if (currentPage === 1)
        setTotalItems(res.data?.length);
    console.log(res.data);
    return res.data;
    },
  {
     staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: !!eventId  && !!salesOrderCode//  only run query if we have an id
  });

  if (isLoading) return <p>Loading...</p>;
 
  return ( 
      <div className="flex flex-col max-w-md mx-auto ">
        
          <div className="text-2xl font-bold font-heading mb-4 text-primary-color text-center">Your tickets</div>       
         
          {/* <EventSummary eventBasic={sampleEvent}/>  */}
      
          {data && <SalesOrderTicket eventBasic={eventDetails} 
              tickets={[{eventItemTypeId: data[currentPage-1].eventItemType.eventItemTypeId, name: data[currentPage-1].eventItemType.name, 
                description:"", cost:0, quantity:1, ticketsSold:-1, totalAllowed:-1 }]} 
              salesOrderCode={data[currentPage-1].qrCode} qrBase64String={data[currentPage-1].qrBase64Image}>
            
            </SalesOrderTicket>
          }
                
         <div className="ml-auto mb-4">
            <AppPagination totalItems={totalItems} currentPage={currentPage} onPageChange={onPageChange} itemsPerPage={1}></AppPagination>
         </div>
          
    </div>
  );
}
