import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { TicketFormValues, Ticket } from "../types/Tickets";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useParams } from "react-router";
import { Button } from "flowbite-react";
import { useState } from "react";
import EventSummary from "../components/EventSummary";

   
export default function OrderSummary() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [error,setError] = useState("");
  
  const  cart = useAppSelector((state:RootState) => state.cart);
  //const user = useAppSelector((state:RootState) => state.auth);
  console.log(cart);
  if (cart.tickets.length === 0) {
    return (
      <div className="p-4"> 
        <h2 className="text-xl font-bold mb-2">No tickets in cart</h2>
        <Button
              className="align-bottom mt-auto align-center ml-4"
                    size="xs"
                    onClick={() => navigate(`/buytickets/${id}`)}>
                    Back to Cart
        </Button>
        {/* <Link to={`/buytickets/${id}`} className="text-primary-color underline">Get Tickets</Link> */}
      </div>
    );
  }
 
  const paymentRequired =cart.tickets.some((t) => t.cost && t.cost > 0);
  let totalAmount = cart.tickets.reduce((total, ticket) => total + (ticket.quantity ? ticket.quantity * ticket.cost : 0), 0);
  const stripeFees = parseFloat((totalAmount * 0.03).toFixed(2));
  const platformFees = 1;
  if (paymentRequired) {
    totalAmount += stripeFees + platformFees;
  }
  console.log("totalAmount", totalAmount);
  console.log("paymentRequired", paymentRequired);
  const handleconfirmOrder = () => {
      
      navigate(`/orderconfirmation/event/${id}/salesOrderCode/ABCDEF12345`);
    
    // axiosClient.post("/orders", {
    //   userId: user.user?.id, // Replace with actual user ID
    //   eventId: id,
    //   tickets: cart.tickets.filter(t=>t.quantity && t.quantity>0).map(t => ({ ticketTypeId: t.id, quantity: t.quantity })),
    //   buyer: {
    //     fullname: cart.fullname,
    //     email: cart.email,
    //   },
    //   totalAmount: totalAmount,
    //   paymentRequired: paymentRequired,
    // }).then((res) => {
    //   console.log("Order created:", res.data);  
    //   navigate(`/orderconfirmation/${id}`);
    // }).catch((error) => {
    //   console.error("Error creating order:", error);
    //   setError("Error creating order. Please try again."+error.message);
    //   //return(<p className="text-red-500">Error creating order. Please try again.</p>)
    //   // Handle error (e.g., show error message to user)
    // });
  }
  return (
      <div className="flex flex-col max-w-md mx-auto mt-6 border border-gray-300 rounded-lg p-6 shadow-lg">
        <EventSummary/>
        <div className="bg-brand-neutral p-4 rounded">
          <div className="text-xl font-bold mb-4 text-primary-color text-center">Order Summary</div>
          {
            cart.tickets.filter(t=>t.quantity && t.quantity>0).map((ticket:Ticket) => (
              <div key={ticket.eventItemTypeId} className="flex justify-between mb-2">
                <span> {ticket.quantity} @ {ticket.name} </span>
                <span>${ticket.cost}</span>
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
      {/* Button */}
      <div className="ml-auto mt-4">
        <Button
              className="ml-auto ml-4"
              onClick={() =>handleconfirmOrder()}>
              {paymentRequired? "Buy Tickets": "Confirm Order"}
        </Button>
      </div>
      {error && <p className="text-red-500 text-sm mt-2">{error}</p>}
    </div>
  );
}

