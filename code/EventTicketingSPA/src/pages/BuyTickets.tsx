// src/pages/Login.jsx
import { useForm, Controller } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import CartTotal from "../components/CartTotal"
import { TicketFormValues, Ticket } from "../types/Tickets";
import {  updatebuyer, updatetickets } from "../features/auth/cartSlice";
import { RootState } from "../app/store";
import { useHistory, useLocation, useParams } from "react-router";
import OrderSummary from "./OrderSummaryDefunct";
import EventSummary from "../components/EventSummary";
import SimulationInfo from "../components/SimulationInfo";
import axiosClient from "../api/axiosClient";
import { useQuery } from "react-query";
import { useEffect, useState } from "react";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import toast, { Toaster } from 'react-hot-toast';
import { SalesOrderErrors } from "../types/Order";
import { loginAsGuest } from "../features/auth/authSlice";
import  Footer from "../components/Footer"
import DomPurify from "dompurify";

const schema = yup.object({
  email: yup.string().required("Email is required").email("Invalid email format"),
  fullname: yup.string().required("Fullname is required"),
  zipCode: yup.string().default(''),
  tickets: yup
    .array()
    .of(
      yup.object({
        eventItemTypeId: yup.number().required(),
        cost: yup.number().required(),
        name: yup.string().required(),
        description: yup.string().required(),
        maxPerOrder:yup.number().optional(),
        ticketsSold:yup.number().required(),
        totalAllowed: yup.number().required(),
        quantity: yup.number()
          .min(0, "Quantity must be at least 0")
          .typeError("Quantity must be a number")
          .required()
          .test('less-than-max-count',
            'Quantity is more than maximum permissible.',
            function(value){
              const {maxPerOrder, ticketsSold, totalAllowed}= this.parent;
              const maxPerOrderOk = (maxPerOrder>0 && value <= maxPerOrder) || maxPerOrder === 0;
              const totalAllowedOk = value <= (totalAllowed - ticketsSold);
              console.log('totalAllowedOk',totalAllowedOk);
                console.log('maxPerOrderOk',maxPerOrderOk);
              return maxPerOrderOk && totalAllowedOk;
            }
          ),   
        })
      ).required()
      .test(
          "at-least-one-ticket",
          "Please select at least one ticket",
          (items) => {
          
            if (!items) return false;
            return items.some((t) => t.quantity && t.quantity > 0);
          }
    ),
});


export default function BuyTickets() {
  
  const location = useLocation();
  const history = useHistory();
  const dispatch = useAppDispatch();
  const  cart = useAppSelector((state:RootState) => state.cart);
  const eventHeaderInfo = useAppSelector((state:RootState) => state.event);
  const user = useAppSelector((state:RootState) =>state.auth);

  const [stripeSessionId,setStripeSessionId] = useState<string | null>(null);

  const ionRouter = useIonRouter();
  const { id, simulationMode ,organizerId} = location.state as any || {};
  const [checkoutError,setcheckoutError] = useState([]);
  console.log('stripe session id in buy tickets',stripeSessionId);
  
  const {
        data: ticketTypesList = [], // provide default empty array
        isLoading,
        error
  } = 
  useQuery(
    ['eventitemtype', id], // structured query key
    async () => {
      console.log("in buy tickets calling backend");
      const res = await axiosClient.get(`/eventitemtype/all/${id}`);
      console.log('Event ticket type details', res?.data);
      return res.data;
    },
    {
      //staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: !!id // only run query if we have an id
    }
  );

  console.log('tickettype is',ticketTypesList)
  const { control,register, reset,handleSubmit,watch,formState: { errors }, setError } = useForm<TicketFormValues>({
      resolver: yupResolver(schema),
      defaultValues: {
        fullname: cart.fullname || user?.user?.name,
        email: cart.email || user?.user?.email,
        zipCode: cart.zipCode || '',
        tickets: cart.tickets.length>0 ? cart.tickets : [] 
      },
        mode: "onChange",          // 👈 validates as user types or changes field
        reValidateMode: "onChange"
    });
  // Add useEffect to reset form when ticketTypesList loads.
  //The ticket type list is not ready when the useform tries to  set default values.
  useEffect(() => {
    if (ticketTypesList && ticketTypesList.length > 0) {
      reset({
        fullname: user.user?.name || cart.fullname || '',
        email: user.user?.email || cart.email || '',
        zipCode: cart.zipCode || '',
        tickets: cart.tickets?.length > 0 
          ? cart.tickets 
          : ticketTypesList.map((t: Ticket) => ({ 
              eventItemTypeId: t.eventItemTypeId, 
              name: t.name, 
              quantity: 0, 
              description: t.description, 
              cost: t.cost ,
              maxPerOrder: t.maxPerOrder,
              totalAllowed: t.totalAllowed,
              ticketsSold: t.ticketsSold,
            }))
        });
    }
  }, [ticketTypesList, cart.tickets, cart.fullname, cart.email, reset]);


  if (error) console.error('Error fetching ticket types:', error);
  if (isLoading) return <p>Loading...</p>;

  const watchedTickets = watch("tickets") ?? [];

  const onSubmit = async  (data: TicketFormValues) => {
    const requiresPayment = paymentNeeded(data.tickets);

    // Custom validation: zipcode required only if payment is needed
    if (requiresPayment && !data.zipCode?.trim()) {
      setError("zipCode", { message: "Zipcode is required" });
      return;
    }

    console.log(errors);
    console.log('submit',data);
    dispatch(updatebuyer({ fullname: data.fullname, email: data.email,zipCode: data.zipCode }));
    dispatch(updatetickets({ tickets: data.tickets }));
    //ionRouter.push(`/ordersummary/${id}`);
    await checkout(data);
  };
 
  const paymentNeeded =  (tickets: TicketFormValues['tickets'] = []) =>
    tickets.some((t: any) => (Number(t?.quantity) || 0) > 0 && (Number(t?.cost) || 0) > 0);
  
  const checkout = async (formData:TicketFormValues) => {       
    try
    {
      //todo:revisit this logic since stripesessionid seems to exist even if we come after timeout
      //so a timeout  order gets replaced status and ticket sold is decremented twice.
      //we can comment this out since  if a user pressed back button , the order will stay in reserved status
      //amnd get picked up by the worker to clean. A new order will be creaed here.
      //reserced
      // if (stripeSessionId)
      // {
      //   console.log('Existing stripe session id found. User most likely pressed back button:'+stripeSessionId);
      //   const resetTickets = await axiosClient.post(`/SalesOrder/ReturnTickets/${stripeSessionId}/Replaced`);
      //   if (resetTickets && resetTickets.status ===200)
      //   {
      //     console.log('Previous tickets associated with session returned to pool successfully.');
      //   }
      //   else
      //   {
      //     console.log('Unable to return previous tickets associated with session. Proceeding may result in overbooking.');
      //     toast.error("Unable to return previous tickets associated with session. Proceeding may result in overbooking.");
      //     return;
      //   }
      // }
      const result = await axiosClient.post("/salesOrder", {
        userId: user == null || user.user?.guest? 0: user.user?.id, 
        eventId: id,
        simulationMode: simulationMode,
        customerId: eventHeaderInfo.eventOrganizerId || organizerId || 0,
        emailAddress: formData.email,
        zipCode: formData.zipCode,
        name: formData.fullname,
        deliveryType :"Email",
        salesOrderItemsError:[],
        salesOrderItems: formData.tickets.filter(t=>t.quantity && t.quantity>0).map(t => ({ eventTicketTypeId: t.eventItemTypeId, quantity: t.quantity, cost: t.cost })),
      },
       {
        headers: {
          'Content-Type': 'application/json',
          // Pass the email in the headers so the backend rate-limiter can see it instantly
          'X-Buyer-Email': formData.email.trim().toLowerCase() 
        }}
    );
      if (result.status !=200)
      {
        toast.error("Order could not be created successfully." +result.status);
      }
      else
      {
        if (result.data.accessToken)
        {
          console.log('guest login detected. Found token');
          dispatch(loginAsGuest(result.data));     
        }
        const guestAlreadyExists = result.data?.guestAlreadyExists;
        console.log('guestAlreadyExists',guestAlreadyExists);

        console.log(`Received 200 from order creation. checking error array...`);
        const salesOrderData = result.data?.salesOrderData;
        console.log('Sales order data isss:', salesOrderData);
        if (salesOrderData?.SalesOrderItemsError && salesOrderData?.SalesOrderItemsError >0)
        {
          setcheckoutError(result.data?.SalesOrderItemsError);
          console.log('Order creation returned errors:', result.data?.SalesOrderItemsError);
          toast.error("There were issues with some items in your order. Please review.");
          return;
        }
        else if (salesOrderData)
        {
          console.log('Successfully created order with orderCode:'+salesOrderData.salesOrderCode);
          if (!paymentNeeded(watchedTickets))
          {
             toast.success("Created order successfully");
             history.push(`/orderconfirmation`, {orderData:salesOrderData, guestAlreadyExists: guestAlreadyExists});
          }
          else
          {
            if (!salesOrderData?.checkoutSessionSecret || !salesOrderData?.checkoutSessionId || 
              !salesOrderData?.checkoutSessionPublishableKey)
            {
              console.log('Unable to proceed to payment due to incomplete setup.');
              toast.error("Unable to proceed to payment due to incomplete setup. Please try again later.");
              return;
            }

            setStripeSessionId(salesOrderData.checkoutSessionId);
            if (guestAlreadyExists)
              sessionStorage.setItem('stripe_checkout_guest_exists', JSON.stringify(guestAlreadyExists));
            console.log('Proceeding to payment with session id:',salesOrderData.checkoutSessionId);
            history.push(`/orderpayment`, {salesOrderData:salesOrderData, id: id, simulationMode: simulationMode});
          }
        }
      }
    }
    catch(error:any)
    {
      toast.error("There was an error creating your order.Please try again." + error.message);
      console.log("Error creating order. Please try again."+error.message);
    }
  }
return (
   <IonPage>
    <IonHeader>
      <AppNavbar />
    </IonHeader>
    <IonContent className="">
    <div className="flex flex-col min-h-full">
      {/* <Toaster position="top-right" /> */}
      <div className="flex flex-col  max-w-xl mx-auto p-4  justify-center">
        <div className="text-3xl font-bold mb-8 text-primary-color text-center">Ticket Types</div>
          <EventSummary/>
          {simulationMode ? <SimulationInfo /> : null}
        {/* <form onSubmit={handleSubmit(
  onSubmit,
  (errors) => console.log("validation errors", errors)
)}> */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {ticketTypesList.map((item: Ticket, index:number) => 
      (
          <div  key={item.eventItemTypeId} className="flex flex-col">
            <div className="flex flex-row">
                <div className="text-l text-secondary-color w-1/2 text-left"
                  dangerouslySetInnerHTML={{ __html: item.name + ': ' + DomPurify.sanitize(item.description) }}/>
                <div className="text-xl text-center text-secondary-color  w-1/3">{item.cost === 0 ? <span className="text-green-500 font-bold">Free</span> : `$${item.cost}`}</div>
                  <div className="text-l text-center text-secondary-color">
                    <input
                      key={item.eventItemTypeId}
                      type="number"
                      {...register(`tickets.${index}.quantity`, { valueAsNumber: true })}
                      className="w-20 border rounded p-1"
                      min={0}
                      disabled={item.ticketsSold >= item.totalAllowed}
                    />
                  </div>  
            </div>
            
            <div className="w-1/3 ml-auto text-right mr-3">
              {item.ticketsSold >=item.totalAllowed && (
                <div className="text-red-500 text-sm ">
                    Sold Out!
                </div>
              )}
              {item.quantity >0 && (item.totalAllowed - item.ticketsSold) >0 
                  && (item.totalAllowed - item.ticketsSold) <= 5 && (      
                <div className="text-red-500 text-sm ">
                    Very few left!
                </div>                  
              )}
              {errors?.tickets?.[index]?.quantity?.message && (
                      <div className="text-red-500 text-sm ">
                        {errors.tickets[index].quantity.message}
                      </div>
                )}
            </div>
          </div>
        ))}

        {errors.tickets && (
          <p className="text-red-500 text-sm mt-2">
              {errors.tickets.message || errors.tickets.root?.message}
          </p>
        )}

        <div className="flex flex-row mt-4 space-x-8">
  
  {/* Left Column: User Info & Zip */}
  <div className="w-1/2 flex flex-col justify-start"> 
    {/* Full Name & Email (Conditional) */}
    {(user.user?.guest || user.user == null) && (
      <>
        <label className="text-sm font-medium">Full Name</label>
        <input type="text" {...register("fullname")} className="border rounded px-3 py-2" />
        {errors.fullname && (
          <p className="text-red-500 text-sm">{errors.fullname?.message}</p>
        )}

        <label className="mt-4 block text-sm font-medium">Email</label>
        <input type="text" {...register("email")} className="border rounded px-3 py-2" />
        {errors.email && (
          <p className="text-red-500 text-sm">{errors.email.message}</p>
        )}
      </>
    )}

    {/* Zipcode (Always visible if payment is needed) */}
    {paymentNeeded(watchedTickets) && (
      <div className={(user.user?.guest || user.user == null) ? "mt-4" : ""}>
        <label className="block text-sm font-medium">Zipcode</label>
        <input type="text" {...register("zipCode")} className="border rounded px-3 py-2 w-full" />
        {errors.zipCode && (
          <p className="text-red-500 text-sm">{errors.zipCode.message}</p>
        )}
      </div>
    )}
    <p className={paymentNeeded(watchedTickets)? "text-[11px] mt-3 max-w-sm mx-auto": "text-[11px] mt-12 max-w-sm mx-auto"}>
        By clicking  {!paymentNeeded(watchedTickets) ? 'Confirm Order' : 'Proceed to payment'}, 
        you explicitly agree to {import.meta.env.VITE_COMPANY_NAME}'s
        
        {' '}<a href="/tos" className="underline hover:text-slate-600">Terms of Service</a>,
        {' '}<a href="/privacypolicy" className="underline hover:text-slate-600">Privacy Policy</a>,{' '}and our non-refundable
        {' '}<a href="/refundpolicy" className="underline hover:text-slate-600">Refund Policy</a> parameters.
      
    </p>

  </div>

  {/* Right Column: Totals and Checkout */}
 
  <div className="flex-1 flex flex-col items-end justify-end"> 
    <div className="w-full text-right">
      <CartTotal control={control} feeMode={eventHeaderInfo.ticketFeeMode || 0} eventId={id}/>
    </div>

    <button type="submit" className="mt-6 bg-brand-dark text-white px-6 py-2 rounded hover:bg-blue-700 font-semibold transition-colors">
      {!paymentNeeded(watchedTickets) ? 'Confirm Order' : 'Proceed to payment'}
    </button>
   
  </div>
  
   
</div>


            

           
  </form>
    
    </div>
     <Footer/>  
    </div>
    </IonContent>
   
    </IonPage>
  );
}
