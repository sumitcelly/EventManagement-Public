import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";

import { useHistory, useLocation, useParams } from "react-router-dom";
import { EventTickets } from "../types/Tickets";
import EventSummary from "../components/EventSummary";
import { useEffect, useState } from "react";
//import AppPagination from "../components/Pagination";
import SalesOrderTicket from "../components/SalesOrderTicket";
import App from "../App";
import AppPagination from "../components/Pagination";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import { Button } from "flowbite-react";
import Footer from "../components/Footer";

export default function TicketDetails() {
  
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const history = useHistory();
  const onPageChange = (page: number)=>{
        console.log("onpagechange",page);
        setCurrentPage(page);
  }

  const location =useLocation();
  //const { eventId, salesOrderCode,salesOrderId,salesOrderStatus } =
  //useParams<{ eventId: string; salesOrderCode: string; salesOrderId: string;salesOrderStatus:string}>();
  //console.log('eventId and salesOrderCode from params', eventId, salesOrderCode);
  //const  eventDetails = useAppSelector((state:RootState) => state.event);
  const salesOrderData:any = location.state || {};
  console.log('Sales order data received',salesOrderData);
  let eventId = 0, salesOrderCode = '', salesOrderId='', salesOrderStatus='';

  if (salesOrderData){
    eventId = salesOrderData.eventId || 0;
    salesOrderCode = salesOrderData.salesOrderCode || '';
    salesOrderId = salesOrderData.salesOrderId || '';
    salesOrderStatus = salesOrderData.salesOrderStatus || '';
  }
   const {encryptedOrderId} = useParams<{ encryptedOrderId: string }>();
   console.log('Encrypted order id from params', encryptedOrderId);


  const {
        data: orderDetails, // provide default empty array
        isLoading:orderLoading,

  } = 
  useQuery(
    ['orderDetails', encryptedOrderId], // structured query key
    async () => {
    
      const urlDecodedOrderId = encryptedOrderId ? decodeURIComponent(encryptedOrderId) : ''; 
      console.log('URL decoded order id', urlDecodedOrderId);
      const res = await axiosClient.get(`/SalesOrder/byEmailLinkId/${encryptedOrderId}`);
      console.log('salesDetails details from backend', res?.data);
      return res.data;
    },
    {
      //staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,
      refetchOnWindowFocus: false,
      enabled: !!encryptedOrderId && eventId ===0//  only run query if we have an id
    }
  );

  const {
        data: eventDetails, // provide default empty array
        isLoading:eventLoading
  } = 
  useQuery(
    ['eventDetails', orderDetails?.eventId || eventId], // structured query key
    async () => {
    
      const idToUse = orderDetails?.eventId || eventId;
      const res = await axiosClient.get(`/events/details/${idToUse}`);
      console.log('Event id details from backend', res?.data);
      return res.data;
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      //refetchOnMount: true,
      //refetchOnWindowFocus: true,
      enabled: (orderDetails?.eventId || eventId) > 0 && (!encryptedOrderId || !orderLoading) //  wait for orderDetails if needed
    }
  );

  const { data, isLoading } = useQuery( ['ticketDetails', orderDetails?.eventId || eventId, orderDetails?.salesOrderCode || salesOrderCode], async () => {
    const idToUse = orderDetails?.eventId || eventId;
    const codeToUse = orderDetails?.salesOrderCode || salesOrderCode;
    const res = await axiosClient.get(`/Ticket/ByEventIdAndSalesOrderQrCode/${idToUse}/${codeToUse}`);
    console.log('user tickets from backend', res?.data);
    if (currentPage === 1)
        setTotalItems(res.data?.length);
   
    return res.data;
    },
  {
     staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      //refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: (orderDetails?.eventId || eventId) > 0 && !!(orderDetails?.salesOrderCode || salesOrderCode) && (!encryptedOrderId || !orderLoading) //  wait for orderDetails if needed
  });
   
  useEffect(() => {
     if (currentPage === 1)
        setTotalItems(data?.length);
   
 
  }, []);
  if ((encryptedOrderId && orderLoading) || eventLoading || isLoading) return <p>Loading...</p>;
  console.log('event details:',eventDetails);
  return ( 
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent>
         <div className="flex flex-col  min-h-full">
          <div className="max-w-md mx-auto">
          <div className="text-2xl font-bold font-heading mb-4 text-primary-color text-center">Your tickets</div>       
        
          {data && <SalesOrderTicket eventBasic={eventDetails} 
              tickets={[{eventItemTypeId: data[currentPage-1].eventItemType.eventItemTypeId, name: data[currentPage-1].eventItemType.name, 
                description:"", cost:0, quantity:1, ticketsSold:-1, totalAllowed:-1 }]}
              errorTicketList={[]} 
              salesOrderCode={data[currentPage-1].qrCode} qrBase64String={data[currentPage-1].qrBase64Image}>
            
            </SalesOrderTicket>
          }
          
         <div className="ml-auto mb-4">
            <AppPagination totalItems={totalItems} currentPage={currentPage} onPageChange={onPageChange} itemsPerPage={1}></AppPagination>
         </div>
         {/* Refund mode must be customer controlled (1)*/}
          {eventDetails?.refundMode ===1 && (orderDetails?.salesOrderStatus || salesOrderStatus) === "PaymentSucceeded" && (
            <div className="ml-auto mt-4">
              <button onClick={()=>history.push(`/refundorder`, 
              {orderId:orderDetails?.salesOrderId || salesOrderId, eventId: eventDetails?.eventId })}
                  className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
                  >
                  Initiate refund
              </button>
            </div>
          )}  
        </div>      
        <Footer/> 
    </div>
    
    </IonContent>
    </IonPage>
  );
}
