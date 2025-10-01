import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { RootState } from "../app/store";
import { useAppSelector } from "../app/hook";
import { useLocation, useParams } from "react-router";
import { Button } from "flowbite-react";
import EventSummary from "../components/EventSummary";
import { resetCart } from "../features/auth/cartSlice";
import { useAppDispatch } from "../app/hook";
import { useEffect, useState } from "react";
import SalesOrderTicket from "../components/SalesOrderTicket";
import { s } from "react-router/dist/development/index-react-server-client-CMphySRb";
import { Ticket } from "../types/Tickets";
import {SalesOrderErrors} from "../types/Order"

export default function OrderConfirmation() {
  

  const [cartTickets,setCartTickets] = useState<Ticket[]>([]);

  const navigate = useNavigate();
  const { eventId } = useParams();
  
  const location = useLocation();
  const salesOrderData = location.state;
  console.log('sales order receipt', salesOrderData);


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
   
    dispatch(resetCart());
  }, [dispatch]);
  console.log('carttickets',cartTickets);
  return (
      <div className="flex flex-col max-w-md mx-auto ">
        <EventSummary/>
        <div className="bg-brand-neutral p-4 rounded mb-4 font-body">  
          <div className="text-xl font-bold font-heading mb-4 text-primary-color text-center">Order Confirmation</div>
          {
            salesOrderData.salesOrderCode?
            (
              <>
                <div className="text-center mb-4">
                  Thank you for your order! You are all set to go to <span className="font-accent text-xl">{event.eventName}</span>.
                </div>
                 {salesOrderData.salesOrderItemsError && salesOrderData.salesOrderItemsError.length > 0 && 
                (
                  <div className="text-center mb-4 text-secondary-color">
                      One or more of your tickets could not be processed:
                    
                      {salesOrderData.salesOrderItemsError.map((item:SalesOrderErrors)=>
                        (
                          <div key={item.eventItemTypeId} className="text-xs">{item.error}: {cartTickets.find(i=>i.eventItemTypeId === item.eventItemTypeId)?.name}</div>
                        )
                      )
                     }  
                  </div>
                )}
                <div className="text-center mb-4">
                  Your order reference code is <span className="font-bold">{salesOrderData.salesOrderCode}</span>
                </div>

               
                <SalesOrderTicket eventBasic={event} tickets={cartTickets} errorTicketList={salesOrderData.salesOrderItemsError} salesOrderCode={salesOrderData.salesOrderCode || ""} 
                                  qrBase64String={salesOrderData.salesOrderQrCodeImage}/>
                <div className="text-center mb-4 mt-2">
                  You will receive an email confirmation to {cart.email} shortly with your e-tickets.
                </div>
                <div>
               {/*This maybe confusing since we are already showing order receipt with qr code at top*/}
                  <Button
                        className="align-bottom ml-auto align-center"
                        size="xs"
                        onClick={() => navigate(`/ticketdetails/${event.eventId}/${salesOrderData.salesOrderCode}`)}>
                        View your Tickets
                  </Button>
                </div>
              </>    
            ):
            (
              <div className="text-center mb-4">
                  Your order could not be processed:
                 
                  {salesOrderData.salesOrderItemsError && 
                      salesOrderData.salesOrderItemsError.map((item:SalesOrderErrors)=>
                    (
                      <div key={item.eventItemTypeId} className="text-xs">{item.error}: {cartTickets.find(i=>i.eventItemTypeId === item.eventItemTypeId)?.name}</div>
                     )
                  )}  
              </div>
            )
          }
        </div>
       
      </div>
  );

  
}

