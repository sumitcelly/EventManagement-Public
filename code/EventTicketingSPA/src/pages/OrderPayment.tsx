import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useHistory, Link } from "react-router-dom";
import { TicketFormValues, Ticket } from "../types/Tickets";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useLocation, useParams } from "react-router";
import { Button } from "flowbite-react";
import { useState } from "react";
import EventSummary from "../components/EventSummary";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
import {loadStripe} from '@stripe/stripe-js';
import {
  EmbeddedCheckoutProvider,
  EmbeddedCheckout
} from '@stripe/react-stripe-js';

const pkStripe =  import.meta.env.VITE_STRIPE_PK;
const stripePromise = loadStripe(pkStripe);

export default function OrderPayment() {
  const history = useHistory();
  const { id } = useParams<{ id: string }>();
  const [error,setError] = useState("");
  
  const  cart = useAppSelector((state:RootState) => state.cart);
  const eventHeaderInfo = useAppSelector((state:RootState) => state.event);
  const user = useAppSelector((state:RootState) =>state.auth);
  
  const stripeAccountId = eventHeaderInfo.organizerStripeAccountId;
  console.log(`stripe account id is ${stripeAccountId}`);

  const location = useLocation();
  const salesOrderData:any = location.state || {};
  console.log('sales order',salesOrderData);
  
  if (!salesOrderData || !salesOrderData.checkoutSessionSecret || !salesOrderData.checkoutSessionId) {
    return (
      <div className="p-4"> 
        <h2 className="text-xl font-bold mb-2">No payment session found. Please try again.</h2>
          <Button
            className="align-bottom mt-auto align-center ml-4"
              size="xs"
              onClick={() => history.push(`/buytickets/${id}`)}>
              Back to Cart
          </Button>
        {/* <Link to={`/buytickets/${id}`} className="text-primary-color underline">Get Tickets</Link> */}
      </div>
    );
  } 
 
  if (cart.tickets.length === 0) {
    return (
      <div className="p-4"> 
        <h2 className="text-xl font-bold mb-2">No tickets in cart</h2>
          <Button
            className="align-bottom mt-auto align-center ml-4"
              size="xs"
              onClick={() => history.push(`/buytickets/${id}`)}>
              Back to Cart
          </Button>
        {/* <Link to={`/buytickets/${id}`} className="text-primary-color underline">Get Tickets</Link> */}
      </div>
    );
  }
 
  const paymentRequired =cart.tickets.some((t) => t.cost && t.cost > 0);

  if (paymentRequired && !stripeAccountId)
  {
    return (
      <div className="p-4"> 
        <h2 className="text-xl text-secondary-color font-bold mb-2">Unable to proceed with order due to incomplete organizer setup. 
          Please only select tickets that do not require a payment.</h2>
          <Button
            className="align-bottom mt-auto align-center ml-4"
              size="xs"
              onClick={() => history.push(`/buytickets/${id}`)}>
              Back to Cart
          </Button>
        </div>
    );
  }

  if (!paymentRequired)
  {
    return (
      <div className="p-4"> 
        <h2 className="text-xl text-secondary-color font-bold mb-2">Unable to proceed with order. Please try again!</h2>
          <Button
            className="align-bottom mt-auto align-center ml-4"
              size="xs"
              onClick={() => history.push(`/buytickets/${id}`)}>
              Back to Cart
          </Button>
        </div>
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
        <div className="bg-brand-neutral p-4 rounded">
          <div className="text-xl font-bold mb-4 text-primary-color text-center">Order Summary</div>
          {
            cart.tickets.filter(t=>t.quantity && t.quantity>0).map((ticket:Ticket) => (
              <div key={ticket.eventItemTypeId} className="flex justify-between mb-2">
                <span> {ticket.quantity} @ {ticket.name} </span>
                <span>${ticket.quantity*ticket.cost}</span>
              </div>
            ))
          }
         
            {paymentRequired && (
              <>
                <div className="flex justify-between pt-2">
                  <span>Stripe fees:</span>
                  <span>${stripeFees}</span>
                </div>
                <div className="flex justify-between  mb-2">
                  <span>Platform fees:</span>
                  <span>${platformFees}</span>
                </div>
              </>
              
            )}
             <div className="flex justify-between font-bold mb-2 pt-2">
               <span>Total:</span>
                 <span>${totalAmount}</span>
             
              {/* <span className="text-sm text-gray-500 italic"> (incl. fees if applicable)</span> */}
              
          </div>
        </div>
        
      {/*Payment summary */}
      <div>
        {paymentRequired && (
          <div className="mt-6 border border-gray-300 rounded-lg p-6 shadow-lg bg-brand-neutral">
            <div className="text-xl font-bold mb-4 text-primary-color text-center">Payment Summary</div>
            <div className="flex justify-between mb-2">
              <span>Amount to be charged:</span>
              <span>${totalAmount}</span>
            </div>
          </div>
        )}
      </div>
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
      {/* Button */}
      {/* <div className="ml-auto mt-4">
        <Button
              className="ml-auto ml-4"
              onClick={() =>handleconfirmOrder()}>
             "Pay"
        </Button>
      </div> */}
      {error && <p className="text-red-500 text-sm mt-2">{error}</p>}
    </div>
    </IonContent>
    </IonPage>
  );
}

