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
import AppNavbar from "../components/Navbarnew";
import {loadStripe} from '@stripe/stripe-js';
import {
  EmbeddedCheckoutProvider,
  EmbeddedCheckout
} from '@stripe/react-stripe-js';
import toast from "react-hot-toast";
import EventDetails from "./EventDetails";

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
  const params = new URLSearchParams(window.location.search);
  const sessionId = params.get('session_id');
  const orderId = params.get('salesOrderId');

  console.log('session id and orderid from url', sessionId, orderId);

  const getSessionStatus = async (sessionId:string, stripeAcctId:string) => 
  {
    try 
    {
      const response = await axiosClient.get<string>(`/payment/checkout-session-status/${sessionId}/${stripeAcctId}`);
      console.log("Session status response:", response.data);
      if (response.data =="paid")
      {
        toast.success("Payment successful! Your order is confirmed.");
        const salesData = await axiosClient.get(`/SalesOrderQrImage/${orderId}`);
        if (salesData && salesData.data) {
          console.log('sales order data', salesData.data);
          history.replace(`/orderconfirmation/event/${eventHeaderInfo.eventId}`, { salesOrderCode: salesData.data.salesOrderCode, salesOrderQrCodeImage: salesData.data.salesOrderQrCodeImage });
        }
      }
      if (response.data =="unpaid")
      {
        toast.error("Payment is still being processed. Please wait sometime.");
        console.log("Payment is still being processed. Please wait sometime.");
        history.replace(`/orderconfirmation/event/${eventHeaderInfo.eventId}`, {  salesOrderId: orderId, paymentPending:true});
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

  // if (sessionId && sessionStatus) {
  //   return (
  //     <IonPage>
  //       <IonHeader>
  //         <AppNavbar />
  //       </IonHeader>
  //       <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
  //         <div className="p-4"> 
  //           {sessionStatus === 'paid' && (
  //           <>            
  //             <h2 className="text-xl font-bold mb-2">Your order is confirmed</h2>
  //             <Button
  //               className="align-bottom mt-auto align-center ml-4"
  //                 size="xs"
  //                 onClick={() => history.push(`/buytickets/${id}`)}
  //                 >
  //                 View your Tickets
  //             </Button>
  //           </>
  //           )}
            
  //           {sessionStatus !== 'unpaid' && (
  //           <>
  //             <h2 className="text-xl text-secondary-color font-bold mb-2">Your order is still under process.</h2>
  //             <Button
  //               className="align-bottom mt-auto align-center ml-4"
  //                 size="xs"
  //                 disabled={sessionStatus === 'unpaid'}  
  //                 onClick={() => history.push(`/buytickets/${id}`)}
  //                 >
  //                 View your Tickets
  //             </Button>
  //           </>
  //           )}
  //        </div>
  //       </IonContent>
  //     </IonPage>
  //   );
  // }

  if (!salesOrderData || !salesOrderData.checkoutSessionSecret || !salesOrderData.checkoutSessionId) {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
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
        <div id="checkout">
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
    </IonContent>
    </IonPage>
  );
}

