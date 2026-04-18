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
import toast, { Toaster } from "react-hot-toast";

export default function TicketDetails() {
  
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [ticketData,setticketData] = useState<any[]>([]);

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
    
     // const urlDecodedOrderId = encryptedOrderId ? decodeURIComponent(encryptedOrderId) : ''; 
      
      try {
        const res = await axiosClient.get(`/SalesOrder/byEmailLinkId/${encryptedOrderId}`);
        console.log('salesDetails details from backend', res?.data);
        setticketData(res?.data?.ticketDetails || []);
        console.log('ticket data set for pagination from encrypted flow', ticketData);
        if (currentPage === 1)
          setTotalItems(res.data?.ticketDetails?.length || 0);

        return res.data;
      }catch (error) {
        console.error('Error fetching order details', error);
        toast.error('Error fetching order details');
        return null;
      }
 
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
  
  const { data, isLoading } = useQuery( ['ticketDetails', eventId, salesOrderCode], async () => {
    //const idToUse = orderDetails?.eventId || eventId;
    //const codeToUse = orderDetails?.salesOrderCode || salesOrderCode;
    const res = await axiosClient.get(`/Ticket/ByEventIdAndSalesOrderQrCode/${eventId}/${salesOrderCode}`);
    console.log('user tickets from backend', res?.data);
    setticketData(res?.data || []);
    console.log('ticket data set for pagination from normal flow', ticketData);
    if (currentPage === 1)
        setTotalItems(res.data?.length);
   
    return res.data;
  },
  {
     staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      //refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: !!eventId && !!salesOrderCode && (!encryptedOrderId || !orderLoading)
      //enabled: (orderDetails?.eventId || eventId) > 0 && !!(orderDetails?.salesOrderCode || salesOrderCode) && (!encryptedOrderId || !orderLoading) //  wait for orderDetails if needed
  });
   
  useEffect(() => {
     if (currentPage === 1)
        setTotalItems(data?.length);
   
 
  }, []);
  if ((encryptedOrderId && orderLoading) || eventLoading || isLoading) return <p>Loading...</p>;
  console.log('event details:',eventDetails);
  if (!eventDetails) {
    return (
      <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">

        <h1 className="text-2xl font-bold mb-4 text-center">Event Not Found</h1>
        <p className="text-gray-500 text-center">We couldn't find the event associated with this ticket.</p>
      </div>
    );
  }
  return ( 
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent>
          <Toaster position="top-right" />
         <div className="flex flex-col  min-h-full">
          <div className="max-w-md mx-auto">
          <div className="text-2xl font-bold font-heading mb-4 text-primary-color text-center">Your tickets</div>       
        
          {ticketData && <SalesOrderTicket eventBasic={eventDetails} 
              tickets={[{eventItemTypeId: ticketData[currentPage-1].eventItemType.eventItemTypeId, name: ticketData[currentPage-1].eventItemType.name, 
                description:"", cost:0, quantity:1, ticketsSold:-1, totalAllowed:-1 }]}
              errorTicketList={[]} 
              salesOrderCode={ticketData[currentPage-1].qrCode} qrBase64String={ticketData[currentPage-1].qrBase64Image}>
            
            </SalesOrderTicket>
          }
          
         <div className="ml-auto mb-4">
            <AppPagination totalItems={totalItems} currentPage={currentPage} onPageChange={onPageChange} itemsPerPage={1}></AppPagination>
         </div>
         
         {/* Ticket Summary */}
         <div className="max-w-xl mx-auto mt-6 w-full">
            <h3 className="text-lg font-bold text-accent-dark mb-3">Ticket Summary</h3>
            <div className="space-y-2">
              {ticketData && Object.values(
                ticketData.reduce((acc: any, ticket: any) => {
                  const typeName = ticket.eventItemType?.name || 'Unknown';
                  const typeId = ticket.eventItemType?.eventItemTypeId;
                  const key = `${typeId}-${typeName}`;
                  
                  if (!acc[key]) {
                    acc[key] = { name: typeName, count: 0, total: 0 };
                  }
                  acc[key].count += 1;
                  acc[key].total += ticket.pricePaid || 0;
                // {
                //   "1-General Admission": { name: "General Admission", count: 3, total: 45 },
                //   "2-VIP": { name: "VIP", count: 1, total: 30 }
                // }
                  return acc;
                }, {})
              ).map((item: any, idx: number) => (
                <div key={idx} className="flex justify-between text-accent-dark border-b pb-2">
                  <span>{item.name}</span>
                  <span>{item.count} ticket{item.count !== 1 ? 's' : ''}</span>
                  <span>${item.total.toFixed(2)}</span>
                </div>
              ))}
            </div>
         </div>
         
         <div className="mt-4 text-accent-dark text-sm font-body space-y-2">

            <div className="flex justify-between">
              <span>Platform Fees:</span>
              <span>${orderDetails?.platformFees?.toFixed(2) || salesOrderData?.platformFees?.toFixed(2) || '0.00'}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Fees:</span>
              <span>${orderDetails?.totalFees.toFixed(2) || salesOrderData?.totalFees?.toFixed(2) || '0.00'}</span>
            </div>
            <div className="flex justify-between">
              <span>Sales Order Total:</span>
              <span>${orderDetails?.salesOrderTotal.toFixed(2) || salesOrderData?.salesOrderTotal?.toFixed(2) || '0.00'}</span>
            </div>
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
