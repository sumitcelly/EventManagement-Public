import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useLocation, useParams } from "react-router";
import { Button, Spinner } from "flowbite-react";
import EventSummary from "../components/EventSummary";
import { resetCart } from "../features/auth/cartSlice";
import { useAppDispatch } from "../app/hook";
import { useEffect, useState } from "react";
import SalesOrderTicket from "../components/SalesOrderTicket";
import { Ticket } from "../types/Tickets";
import {SalesOrderErrors} from "../types/Order"
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
import { set } from "react-hook-form";

export default function OrderConfirmation() {
  

  const [cartTickets,setCartTickets] = useState<Ticket[]>([]);
  const user = useAppSelector((state:RootState) =>state.auth);
  const ionRouter = useIonRouter();
  const location = useLocation();
  const [salesOrderData,setSalesOrderData] = useState<any>(location.state || {});
  const [checkingPaymentStatus,setCheckingPaymentStatus] = useState<boolean>(false);
  const [orderStatus,setOrderStatus] = useState<string>(salesOrderData.paymentPending === true || 
                                                        !salesOrderData.salesOrderCode
                                                        ? "Pending":"Success");

  console.log('sales order receipt order code and data', salesOrderData.salesOrderCode, salesOrderData);


  const  event = useAppSelector((state:RootState) => state.event);
  const cart  = useAppSelector((state:RootState) => state.cart);
  const dispatch = useAppDispatch();
 

 //do not use dispatch directly in the function body
 //as it will be called on every render causing infinite loop
 //use useEffect to call it only once when the component mounts
   useEffect(() => {
    //need to review this logic. Storing cart in local state to avoid issues with cart being reset
    //before this component is rendered. Need to find a better solution.
    //
    setCartTickets([...cart.tickets]);
   //this resets the cart and any reliance on the cart to show tickets details fails.
   //maybe needs to be another redux for order but need not persist it.
    dispatch(resetCart());
  }, [dispatch]);

  useEffect(() => {
    //return if payment is not pending and order is already generated
    if  (!salesOrderData.paymentPending && salesOrderData.salesOrderCode) return;
    let attempts = 0;
    setCheckingPaymentStatus(true);
    const interval = setInterval(async () => {
      attempts++;
     
      // Check YOUR database, not Stripe, to see if the Webhook finished
      try
      {
        const res = await axiosClient.get(`/salesorderstatus/${salesOrderData.salesOrderId}`);
        const data = res?.data;
        if (data) {
          console.log('sales order status check', res.data);
        if ((res.data.paid) || attempts > 20) {
          clearInterval(interval);
          setCheckingPaymentStatus(false);
          setOrderStatus(data.paid ? 'Success' : 'Timeout');
          if (data.paid) {
            setSalesOrderData((prevData:any) => ({
              ...prevData,
              paymentPending: false,
              salesOrderCode: data.salesOrderCode,
              salesOrderQrCodeImage: data.salesOrderQrCodeImage
            }));
          }
        }
      }
      else
      {
        setCheckingPaymentStatus(false);
        clearInterval(interval);
        console.error('No data returned from sales order status check');
      }
      }
      catch(error)
      {
        setCheckingPaymentStatus(false);
        clearInterval(interval);
        console.error('Error checking sales order status', error);
      }
    }, 5000); // Check every second for 10 seconds

    return () => clearInterval(interval);
  }, [salesOrderData]);

  console.log('carttickets',cartTickets);
  return (
    <IonPage>
      <IonHeader>
         <AppNavbar />
       </IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
      <div className="flex flex-col max-w-md mx-auto ">
        <EventSummary/>
        <div className="bg-brand-neutral p-4 rounded mb-4 font-body">  
          <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center">Order Confirmation</div>
            {checkingPaymentStatus?
            (
              <>
                {/* Flowbite Spinner */}
                <Spinner  aria-label="Loading payment status" size="xl" color="info"/>
                <p className="mt-4 text-lg font-medium text-gray-600">
                  Finalizing your tickets...
                </p>
                <p className="text-sm text-gray-500">
                  Please don't close this window.
                </p>
              </>         
            ):
            (
              <div className="animate-in fade-in duration-500">
              {orderStatus === 'Success' ? (
              <>
                {salesOrderData.salesOrderCode ? (
                  <>
                  <div className="text-center mb-4">
                    Thank you for your order! You are all set to go to <span className="font-accent text-xl">{event.eventName}</span>.
                  </div>
                
                  <div className="text-center mb-4">
                    Your order reference code is <span className="font-bold">{salesOrderData.salesOrderCode}</span>
                  </div>               
                  <SalesOrderTicket eventBasic={event} tickets={cartTickets} errorTicketList={[]} 
                        salesOrderCode={salesOrderData.salesOrderCode || ""} 
                        qrBase64String={salesOrderData.salesOrderQrCodeImage}/>
                  <div className="text-center mb-4 mt-2">
                    You will receive an email confirmation to <span className="font-bold"> {user.user?.email || cart.email}</span> shortly with your e-tickets.
                  </div>
                  <div>
                    <Button
                          className="align-bottom ml-auto align-center"
                          size="xs"
                          onClick={() => ionRouter.push(`/ticketdetails/${event.eventId}/${salesOrderData.salesOrderCode}`)}>
                          View your Tickets
                    </Button>
                  </div>
                  </>
                ) : (
                   <div className="text-red-600">
                    <h2 className="text-2xl font-bold">Order Delayed</h2>
                    <p className="mt-2">Your payment succeeded but there is a delay in order generation. Please check your email in a few minutes.</p>
                  </div>
                )}
                </>
                ) : (
                  <div className="text-red-600">
                    <h2 className="text-2xl font-bold">Verification Timeout</h2>
                    <p className="mt-2">We're taking longer than usual. Please check your email in a few minutes.</p>
                  </div>
                )}
              </div>
            )}
          
        </div>      
      </div>
      </IonContent>
      </IonPage>
  );

  
}

