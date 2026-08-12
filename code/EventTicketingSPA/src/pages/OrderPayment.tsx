import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useHistory, Link } from "react-router-dom";
import { TicketFormValues, Ticket } from "../types/Tickets";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useLocation, useParams } from "react-router";
import { Button } from "flowbite-react";
import { useState, useEffect } from "react";
import EventSummary from "../components/EventSummary";
import SimulationInfo from "../components/SimulationInfo";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import {loadStripe} from '@stripe/stripe-js';
import {
  EmbeddedCheckoutProvider,
  EmbeddedCheckout
} from '@stripe/react-stripe-js';
import toast, { Toaster } from "react-hot-toast";
import CountdownMinutes from "../components/CountdownMinutes";
import Footer from "../components/Footer";



export default function OrderPayment() {
  const history = useHistory();
  const  cart = useAppSelector((state:RootState) => state.cart);
  const eventHeaderInfo = useAppSelector((state:RootState) => state.event);
 
  const location = useLocation();
  const {salesOrderData,id, simulationMode} = location.state as any || {};
  console.log('sales order, simulation mode',salesOrderData, simulationMode);

  const organizerId =  eventHeaderInfo.eventOrganizerId;
  console.log(`stripe account id is ${organizerId}`);

  const pkStripe =  salesOrderData?.checkoutSessionPublishableKey || import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || "";
  const stripePromise = loadStripe(pkStripe);

  //these will obtain from the url params after redirection from stripe checkout
  const params = new URLSearchParams(window.location.search);
  const sessionId = params.get('session_id');
  const orderId = params.get('salesOrderId');

  console.log('session id and orderid from url', sessionId, orderId);

  let underProcess =false
  const getSessionStatus = async (sessionId:string, organizerId:string) => 
  {
    try 
    {
      if (underProcess)
        return;
      underProcess = true;
      const response = await axiosClient.get(`/payment/checkout-session-status/${sessionId}/${organizerId}`);
      console.log("Session status response:", response.data);
      const savedGuestExists = sessionStorage.getItem('stripe_checkout_guest_exists');
      const guestAlreadyExists = savedGuestExists && savedGuestExists !== "undefined"? JSON.parse(savedGuestExists) : false;
      sessionStorage.removeItem('stripe_checkout_guest_exists');
      console.log('guestexists:payment',guestAlreadyExists);

      if (response.data =="paid")
      {
        //blocking the toast message since we are redirecting to order confirmation page and showing the message there.
        //toast.success("Payment successful! Your order is confirmed.",{duration: 5000});
        if (!guestAlreadyExists)
        {
          const salesData = await axiosClient.get(`/SalesOrder/SalesOrderPostPaymentDetails/${orderId}`);
          if (salesData && salesData.data) {
            console.log('sales order data', salesData.data);
            //We only gnerate order after payment is confirmed by the webhook. The return url coming back from stripe
            //should find the order already generated. But if the webhook is delayed we may not have the ordercode in the db yet.
            //In that case we redirect to order confirmation page and let that check 
            // for order status since ordercode will be empty.
            history.push(`/orderconfirmation`, 
              { orderData:{paymentPending: false,
                salesOrderCode: salesData.data?.salesOrderCode, 
                salesOrderQrCodeImage: salesData.data?.qrImage,
                salesOrderId: orderId,
                salesOrderTotal: salesData.data?.salesOrderTotal,
                platformFees: salesData.data?.platformFees,
                totalFees: salesData.data?.totalFees,
                salesTax:salesData.data?.salesTax || 0,
                orderRequiredPayment:true},
                guestAlreadyExists: guestAlreadyExists
              });
              return;
          }
        }
        else
        {
          console.log('Skipping order details retrieve since we will not show order code.');
           history.push(`/orderconfirmation`, 
              { orderData:
                {
                  paymentPending: false,
                    salesOrderId: orderId,
                  orderRequiredPayment:true
                }  ,
                guestAlreadyExists: true
              });
            return;
        }
      }
      if (response.data =="unpaid")
      {
        toast.error("Payment is still being processed. Please wait sometime.");
        console.log("Payment is still being processed. Please wait sometime.");
        history.push(`/orderconfirmation`, 
          {salesOrderData:{ orderRequiredPayment:true, salesOrderId: orderId, paymentPending:true},
            guestAlreadyExists: guestAlreadyExists  
        });
        return;
      }
      toast.error("Invalid payment session status. Please try again.");
    
    } 
    catch (error) {
      console.error("Error fetching session status:", error);
      toast.error("Error fetching session status. Please try again." +error);
    }
  }

  useEffect(() => {
    if (sessionId && organizerId) {
      getSessionStatus(sessionId, String(organizerId));
    }
  }, [sessionId, organizerId]);

  
  if (!salesOrderData || !salesOrderData.checkoutSessionSecret || !salesOrderData.checkoutSessionId) {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
          {/* <Toaster position="top-right" /> */}
          <div className="p-4"> 
            <h2 className="text-xl font-bold mb-2">No payment session found. Please try again.</h2>
              <Button
                className="align-bottom mt-auto align-center ml-4"
                  size="xs"
                  onClick={() => history.push(`/buytickets`,{id:id})}
                  >
                  Back to Cart
              </Button>
            {/* <Link to={`/buytickets/${id}`} className="text-primary-color underline">Get Tickets</Link> */}
          </div>
        </IonContent>
      </IonPage>
    );
  } 
 
  
 
  const paymentRequired =cart.tickets.some((t) => t.cost && t.cost > 0);

  if (paymentRequired && !organizerId)
  {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
          <div className="p-4"> 
            <h2 className="text-xl text-secondary-color font-bold mb-2">Unable to proceed with order due to incomplete organizer setup. 
              Please only select tickets that do not require a payment.</h2>
              <Button
                className="align-bottom mt-auto align-center ml-4"
                  size="xs"
                  onClick={() => history.push(`/buytickets`,{id:id})}
                  >
                  Back to Cart
              </Button>
            </div>
        </IonContent>
      </IonPage>
    );
  }

  if (!paymentRequired)
  {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
          <div className="p-4"> 
            <h2 className="text-xl text-secondary-color font-bold mb-2">Unable to proceed with order. Please try again!</h2>
              <Button
                className="align-bottom mt-auto align-center ml-4"
                  size="xs"
                  onClick={() => history.push(`/buytickets`,{id:id})}
                  >
                  Back to Cart
              </Button>
            </div>
        </IonContent>
      </IonPage>
    );
  }

  if (cart.tickets.length === 0)
  {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
          <div className="p-4"> 
            <h2 className="text-xl font-bold mb-2">No tickets in cart</h2>
              <Button
                className="align-bottom mt-auto align-center ml-4"
                  size="xs"
                  onClick={() => history.push(`/buytickets`,{id:id})}
                  >
                  Back to Cart
              </Button>
            </div>
        </IonContent>
      </IonPage>
    );
  }

  let totalAmount = cart.tickets.reduce((total, ticket) => total + (ticket.quantity ? ticket.quantity * ticket.cost : 0), 0);
  const stripeFees = parseFloat((totalAmount * 0.029).toFixed(2));
  const platformFees =  parseFloat((totalAmount * 0.03).toFixed(2));
  if (paymentRequired) {
    totalAmount += stripeFees + platformFees;
  }
  console.log("totalAmount", totalAmount);
  console.log("paymentRequired", paymentRequired);

  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
    <IonContent className="ion-padding flex flex-col justify-center items-center h-full">

      <div className="flex flex-col max-w-md mx-auto mt-6 border border-gray-300 rounded-lg p-6 shadow-lg">
        <EventSummary/>
        {simulationMode ? <SimulationInfo /> : null}
        <div className="bg-brand-neutral rounded">
        <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center mt-2">Complete your Payment</div>
        <div className="text-center text-bold mb-4">
        <CountdownMinutes displayString="Payment window expires in" initialMinutes={5} timerExpiredCallback={()=>
          {
            toast.error("Payment window has expired. Please try again.");
            axiosClient.post(`/SalesOrder/ReturnTickets/${salesOrderData.salesOrderId}/Timedout`)
            .then(() => {
              history.replace(`/eventDetails/${eventHeaderInfo.eventId}`);
            })
            .catch((error) => {
              console.error("Error cancelling checkout session:", error);
              history.replace(`/buytickets`,{id: eventHeaderInfo.eventId});
            });
          }
        }/>
        </div>
        <div id="checkout" className="mt-4">
          <EmbeddedCheckoutProvider
            stripe={stripePromise}
            options={{       
              clientSecret: salesOrderData.checkoutSessionSecret || "",   
          
            }}
          >
            <EmbeddedCheckout />
          </EmbeddedCheckoutProvider>
        </div>
      </div>
        
    
      {/* {error && <p className="text-red-500 text-sm mt-2">{error}</p>} */}
    </div>
    <Footer/>
    </IonContent>
    </IonPage>
  );
}

