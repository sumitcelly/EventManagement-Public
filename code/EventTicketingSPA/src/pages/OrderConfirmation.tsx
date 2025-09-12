import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useParams } from "react-router";
import { Button } from "flowbite-react";
import EventSummary from "./EventSummary";
import { resetCart } from "../features/auth/cartSlice";
import { useAppDispatch } from "../app/hook";
import { useEffect } from "react";


   
export default function OrderConfirmation() {
  const navigate = useNavigate();
  const { eventId, salesOrderCode } = useParams();
  const  event = useAppSelector((state:RootState) => state.event);
  const cart  = useAppSelector((state:RootState) => state.cart);
  const dispatch = useAppDispatch();
 //do not use dispatch directly in the function body
 //as it will be called on every render causing infinite loop
 //use useEffect to call it only once when the component mounts
   useEffect(() => {
    dispatch(resetCart());
  }, [dispatch]);

  return (
      <div className="flex flex-col max-w-md mx-auto mt-6 border border-gray-300 rounded-lg p-6 shadow-lg">
        <EventSummary/>
        <div className="bg-brand-neutral p-4 rounded mb-4 font-body">  
          <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center">Order Confirmation</div>
          <div className="text-center mb-4">
            Thank you for your order! You are all set to go to <span className="font-accent text-xl">{event.eventName}</span>.
          </div>
          <div className="text-center mb-4">
            Your order reference code is <span className="font-bold">{salesOrderCode}</span>
          </div>
          <div className="text-center mb-4">
            You will receive an email confirmation to {cart.email} shortly with your e-tickets.
          </div>
         </div>
         <div>
          <Button
                className="align-bottom ml-auto align-center"
                size="xs"
                onClick={() => navigate(`/tickets/${event.eventId}`)}>
                View your Tickets
          </Button>
        </div>
      </div>
  );

  
}

