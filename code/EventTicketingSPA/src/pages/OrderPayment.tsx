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

const pkStripe =  import.meta.env.VITE_STRIPE_PK;
const stripePromise = loadStripe(pkStripe);

export default function OrderPayment() {
  const history = useHistory();
  const { id } = useParams<{ id: string }>();

  const [sessionStatus, setSessionStatus] = useState<string | null>(null);
  
  const  cart = useAppSelector((state:RootState) => state.cart);
  const eventHeaderInfo = useAppSelector((state:RootState) => state.event);
  const user = useAppSelector((state:RootState) =>state.auth);
  
  const stripeAccountId = eventHeaderInfo.organizerStripeAccountId;
  console.log(`stripe account id is ${stripeAccountId}`);

  const location = useLocation();
  const salesOrderData:any = location.state || {};
  console.log('sales order',salesOrderData);

  //these will obtain from the url params after redirection from stripe checkout
  const params = new URLSearchParams(window.location.search);
  const sessionId = params.get('session_id');
  const orderId = params.get('salesOrderId');

  console.log('session id and orderid from url', sessionId, orderId);

  const getSessionStatus = async (sessionId:string, stripeAcctId:string) => 
  {
    try 
    {
      const response = await axiosClient.get(`/payment/checkout-session-status/${sessionId}/${stripeAcctId}`);
      console.log("Session status response:", response.data);
      if (response.data =="paid")
      {
        toast.success("Payment successful! Your order is confirmed.");
        const salesData = await axiosClient.get(`/SalesOrderQrImage/${orderId}`);
        if (salesData && salesData.data) {
          console.log('sales order data', salesData.data);
          //We only gnerate order after payment is confirmed by the webhook. The return url coming back from stripe
          //should find the order already generated. But if the webhook is delayed we may not have the ordercode in the db yet.
          //In that case we redirect to order confirmation page and let that check 
          // for order status since ordercode will be empty.
          history.replace(`/orderconfirmation/event/${eventHeaderInfo.eventId}`, 
            { paymentPending: false,
               salesOrderCode: salesData.data?.salesOrderCode, 
               salesOrderQrCodeImage: salesData.data?.qrImage,
               salesOrderId: orderId,
               salesOrderTotal: salesData.data?.salesOrderTotal,
               platformFees: salesData.data?.platformFees,
               totalFees: salesData.data?.totalFees,
               paymentNeeded:true
             });
        }
      }
      if (response.data =="unpaid")
      {
        toast.error("Payment is still being processed. Please wait sometime.");
        console.log("Payment is still being processed. Please wait sometime.");
        history.replace(`/orderconfirmation/event/${eventHeaderInfo.eventId}`, { paymentNeeded:true, salesOrderId: orderId, paymentPending:true});
      }
      toast.error("Invalid payment session status. Please try again.");
      //setSessionStatus(response.data);
      return response.data;
    } 
    catch (error) {
      console.error("Error fetching session status:", error);
      toast.error("Error fetching session status. Please try again." +error);
    }
  }

  useEffect(() => {
    if (sessionId && stripeAccountId) {
      getSessionStatus(sessionId, stripeAccountId);
    }
  }, [sessionId, stripeAccountId]);

  
  if (!salesOrderData || !salesOrderData.checkoutSessionSecret || !salesOrderData.checkoutSessionId) {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
          <Toaster position="top-right" />
          <div className="p-4"> 
            <h2 className="text-xl font-bold mb-2">No payment session found. Please try again.</h2>
              <Button
                className="align-bottom mt-auto align-center ml-4"
                  size="xs"
                  onClick={() => history.push(`/buytickets/${id}`)}
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

  if (paymentRequired && !stripeAccountId)
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
                  onClick={() => history.push(`/buytickets/${id}`)}
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
                  onClick={() => history.push(`/buytickets/${id}`)}
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
                  onClick={() => history.push(`/buytickets/${id}`)}
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
        <div className="bg-brand-neutral rounded">
        <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center mt-2">Complete your Payment</div>
        <div className="text-center text-bold mb-4">
        <CountdownMinutes displayString="Payment window expires in" initialMinutes={5} timerExpiredCallback={()=>
          {
            toast.error("Payment window has expired. Please try again.");
            axiosClient.post(`/SalesOrder/ReturnTickets/${salesOrderData.checkoutSessionId}/Timedout`)
            .then(() => {
              history.replace(`/eventDetails/${eventHeaderInfo.eventId}`);
            })
            .catch((error) => {
              console.error("Error cancelling checkout session:", error);
              history.replace(`/buytickets/${eventHeaderInfo.eventId}`);
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

