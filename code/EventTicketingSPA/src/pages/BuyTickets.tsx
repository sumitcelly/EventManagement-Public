// src/pages/Login.jsx
import { useForm, Controller } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import CartTotal from "../components/CartTotal"
import { TicketFormValues, Ticket } from "../types/Tickets";
import {  updatebuyer, updatetickets } from "../features/auth/cartSlice";
import { RootState } from "../app/store";
import { useNavigate, useParams } from "react-router";
import OrderSummary from "./OrderSummary";
import EventSummary from "../components/EventSummary";
import axiosClient from "../api/axiosClient";
import { useQuery } from "react-query";
import { useEffect } from "react";


const schema = yup.object({
  email: yup.string().required("Email is required"),
  fullname: yup.string().required("Fullname is required"),
  tickets: yup
    .array()
    .of(
      yup.object({
        eventItemTypeId: yup.number().required(),
        cost: yup.number().required(),
        name: yup.string().required(),
        description: yup.string().required(),
        maxPerOrder:yup.number().optional(),
        quantity: yup.number()
          .min(0, "Quantity must be at least 0")
          .typeError("Quantity must be a number")
          .required()
          .test('less-than-max-count',
            'Quantity is more than max allowed for order',
            function(value){
              const {maxPerOrder}= this.parent;
              return (maxPerOrder>0 && value <= maxPerOrder) || maxPerOrder === 0;
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

  const dispatch = useAppDispatch();
  const  cart = useAppSelector((state:RootState) => state.cart);
  const navigate = useNavigate();
  const { id } = useParams();

  
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
      //refetchOnMount: 'always',
      refetchOnWindowFocus: false,
      enabled: !!id // only run query if we have an id
    }
  );

  console.log('tickettype is',ticketTypesList)
  const { control,register, reset,handleSubmit,formState: { errors } } = useForm<TicketFormValues>({
      resolver: yupResolver(schema),
      defaultValues: {
        fullname: cart.fullname || '',
        email: cart.email || '',
        tickets: cart.tickets.length>0 ? cart.tickets : [] 
      },
       mode: 'onSubmit'
    });
  // Add useEffect to reset form when ticketTypesList loads.
  //The ticket type list is not ready when the useform tries to  set default values.
  useEffect(() => {
    if (ticketTypesList && ticketTypesList.length > 0) {
      reset({
        fullname: cart.fullname || '',
        email: cart.email || '',
        tickets: cart.tickets.length > 0 
          ? cart.tickets 
          : ticketTypesList.map((t: Ticket) => ({ 
              eventItemTypeId: t.eventItemTypeId, 
              name: t.name, 
              quantity: 0, 
              description: t.description, 
              cost: t.cost ,
              maxPerOrder: t.maxPerOrder
            }))
      });
    }
  }, [ticketTypesList, cart.tickets, cart.fullname, cart.email, reset]);


  if (error) console.error('Error fetching ticket types:', error);
  if (isLoading) return <p>Loading...</p>;

  
  
  const onSubmit = (data: TicketFormValues) => {
    console.log(errors);
    console.log('submite',data);
    dispatch(updatebuyer({ fullname: data.fullname, email: data.email }));
    dispatch(updatetickets({ tickets: data.tickets }));
    navigate(`/ordersummary/${id}`);
  };
 
return (
   
      <div className="flex flex-col  max-w-xl mx-auto p-4  justify-center">
        <div className="text-3xl font-bold mb-8 text-primary-color text-center">Ticket Types</div>
          <EventSummary/>
        {/* <form onSubmit={handleSubmit(
  (data) => console.log("submit fired!", data),
  (errors) => console.log("validation errors", errors)
)}></form> */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {ticketTypesList.map((item: Ticket, index:number) => 
            (
                <div  key={item.eventItemTypeId} className="flex flex-col">
                  <div className="flex flex-row">
                      <div className="text-l text-secondary-color w-1/2 text-left">{item.name}:  {item.description}</div>
                      <div className="text-xl text-center text-secondary-color  w-1/3">{item.cost}</div>
                      <div className="text-l text-center text-secondary-color">
                          <input
                            key={item.eventItemTypeId}
                            type="number"
                            {...register(`tickets.${index}.quantity`, { valueAsNumber: true })}
                            className="w-20 border rounded p-1"
                            min={0}
                          />
                      </div>  
                    
                  </div>
                  
                  <div className="w-1/3 ml-auto text-right">
                  {errors?.tickets?.[index]?.quantity?.message && (
                          <div className="text-left text-red-500 text-sm ">
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

            <div className="flex flex-row mt-4 space-x-4">

             <div className="w-1/2 flex flex-col border-gray-300 justify-center">
              {/* Fullname control*/}
              <label className="text-sm font-medium">Full Name</label>
                <input
                  type="text"
                  {...register("fullname")}
                  className="border rounded px-3 py-2"
                />
                {errors.email && (
                  <p className="text-red-500 text-sm">{errors.email.message}</p>
                )}
              
                {/* Email control*/}
                <label className="mt-3 block text-sm font-medium">Email</label>
                <input
                  type="text"
                  {...register("email")}
                  className="border rounded px-3 py-2"
                />
                {errors.email && (
                  <p className="text-red-500 text-sm">{errors.email.message}</p>
                )}
              </div>
              {/* Cart total and checkout */}
              <div className="ml-auto mt-auto w-1/2 flex flex-col mt-2 ">
                <div className="ml-auto"><CartTotal control={control}/></div>
                <button
                    type="submit"                         
                    className="mt-3  ml-auto bg-brand-dark text-white px-4 
                        py-2 rounded hover:bg-blue-700">
                    Checkout
                </button>
                {/* By clicking "Checkout", you agree to our Terms of Service and Privacy Policy. */}
            </div>
          </div>
           
        </form>
    
    </div>
   
  );
}
