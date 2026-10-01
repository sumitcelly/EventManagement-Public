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
import AppNavbar from "../components/Navbar";
import { set } from "react-hook-form";
import Footer from "../components/Footer";

export default function OrderConfirmation() {
  
  const history=useHistory();
  const [cartTickets,setCartTickets] = useState<Ticket[]>([]);
  const user = useAppSelector((state:RootState) =>state.auth);
  const ionRouter = useIonRouter();
  const location = useLocation();
  const {orderData, guestAlreadyExists} = location.state as any || {};
  const [salesOrderData,setSalesOrderData] = useState<any>(orderData || {});
  const [checkingPaymentStatus,setCheckingPaymentStatus] = useState<boolean>(false);
  const [orderStatus,setOrderStatus] = useState<string>(salesOrderData.paymentPending === true 
                                                        ? "Pending":"Success");

  console.log('sales order receipt order code and data', salesOrderData.salesOrderCode, salesOrderData);

  const showOrderCode = !guestAlreadyExists;
  const  event = useAppSelector((state:RootState) => state.event);
  const cart  = useAppSelector((state:RootState) => state.cart);
  const dispatch = useAppDispatch();
 
  const orderSuccessStatus = salesOrderData.orderRequiredPayment ? "PaymentSucceeded":"OrderCompleted";

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
    //ordercode generation can be delayed if webhook processing is delayed
    //so this component will check for order status until payment is confirmed by webhook
    //which should have generated the order already
    if  (!salesOrderData.paymentPending) return;
    let attempts = 0;
    setCheckingPaymentStatus(true);
    const interval = setInterval(async () => {
      attempts++;
     
      // Check YOUR database, not Stripe, to see if the Webhook finished
      try
      {
        const res = await axiosClient.get(`/salesorder/salesorderstatus/${salesOrderData.salesOrderId}`);
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
              salesOrderTotal: data.salesOrderTotal,
              platformFees: data.platformFees,
              totalFees: data.totalFees,
              salesTax: data?.salesTax || 0
            }));
            console.log('Payment confirmed, updated sales order data', salesOrderData);
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
      <IonContent>
        <div className="flex flex-col  min-h-full">
    
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
                  {!showOrderCode ?(
                      <>
                      <div className="text-center mb-4">
                        Thank you for your order! You are all set to go to <span className="font-accent text-xl">{event.eventName}</span>.
                      </div>
                        <div className="text-center mb-4">
                        Please check your email <span className="font-bold"> {user.user?.email || cart.email}</span> for your order confirmation and tickets.
                        You can also login to your account to view your tickets.
                      </div>
                    </>
                  ):
                  (
                    salesOrderData.salesOrderCode? (
                    <>
                      <div className="text-center mb-4">
                        Thank you for your order! You are all set to go to <span className="font-accent text-xl">{event.eventName}</span>.
                      </div>
                      <div className="text-center mb-4">
                        Your order reference code is <span className="font-bold">{salesOrderData.salesOrderCode}</span>
                      </div>     
                      {salesOrderData.salesOrderCode.startsWith("TESTTICKET") && (
                        <div className="text-center mb-4 text-red-600">
                          
                          <div className="mt-4 p-4 text-left max-w-md mx-auto bg-red-50 rounded-xl border border-red-200 shadow-sm flex flex-col space-y-3">
                            <div className="flex items-center space-x-2 text-sm font-bold text-red-900 justify-center sm:justify-start">
                              <span>📊</span>
                              <h4>Verify This Inside Stripe</h4>
                            </div>
                            <p className="text-xs text-red-800/90 leading-relaxed text-center sm:text-left">
                              Your checkout pipeline is completely functional! Because this was a sandbox simulation, no real currency was moved. To audit the transaction log data:
                            </p>
                            <ol className="text-xs text-red-900/90 space-y-2 pl-5 list-decimal leading-relaxed">
                              <li>
                                Log into your primary 
                                <a href="https://stripe.com" target="_blank" rel="noreferrer" className="underline font-semibold hover:text-red-700">Stripe Dashboard</a>.
                              </li>
                              <li>
                                Locate the global <strong className="text-red-950 font-bold">"Test Mode"</strong> toggle switch in the upper-right corner and flip it <strong className="text-red-950 font-bold">ON</strong>.
                              </li>
                              <li>
                                Navigate straight to the <strong className="text-red-950 font-bold">Payments</strong> tab to view your simulated checkout ledger.
                              </li>
                            </ol>            
                          </div>
                        </div>
                      )}          
                      {/* <SalesOrderTicket eventBasic={event} tickets={cartTickets} errorTicketList={[]} 
                            salesOrderCode={salesOrderData.salesOrderCode || ""} 
                            qrBase64String={salesOrderData.salesOrderQrCodeImage}/> */}
                      <div className="text-center mb-4 mt-2">
                        You will receive an email confirmation to <span className="font-bold"> {user.user?.email || cart.email}</span> shortly with your e-tickets.
                      </div>
                      <div>
                        <Button
                          className="align-bottom ml-auto align-center"
                          size="xs"
                          onClick={(e) => 
                          {
                            //e.stopPropagation();
                            history.push(`/ticketdetails`,
                            {
                                eventId: event.eventId,
                                salesOrderCode: salesOrderData.salesOrderCode,
                                salesOrderId:salesOrderData.salesOrderId,
                                salesOrderStatus: orderSuccessStatus,
                                salesOrderTotal : salesOrderData.salesOrderTotal || 0,
                                totalFees : salesOrderData.totalFees || 0,
                                platformFees : salesOrderData.platformFees || 0,
                                salesTax:salesOrderData.salesTax ||0
                            });
                          }}>
                            View your Tickets
                        </Button>
                      </div>
                      {/*End of showordercode*/}
                        
                    </>):(
          
                      <>
                      {/* This case should be rare and only happen if there is a delay in order generation after payment confirmation. Webhook processing delay can cause this. */}
                     
                      <div className="text-red-600">
                        <h2 className="text-2xl font-bold">Order Delayed</h2>
                        <p className="mt-2">Your payment succeeded but there is a delay in order generation. Please check your email in a few minutes.</p>
                      </div>
                    </>
                  ))}
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
        <Footer/>
      </div>
      </IonContent>
      </IonPage>
  );

  
}

