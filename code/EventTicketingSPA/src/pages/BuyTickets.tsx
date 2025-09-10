// src/pages/Login.jsx
import { useForm, Controller } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import CartTotal from "./CartTotal"
import { TicketFormValues, Ticket } from "../types/Tickets";
import {  updatebuyer, updatetickets } from "../features/auth/cartSlice";
import { RootState } from "../app/store";
import { useNavigate, useParams } from "react-router";
import { n } from "react-router/dist/development/index-react-server-client-CMphySRb";


const schema = yup.object({
  email: yup.string().required("Email is required"),
  fullname: yup.string().required("Fullname is required"),
  tickets: yup
    .array()
    .of(
      yup.object({
        id: yup.number().required(),
        cost: yup.number().required(),
        name: yup.string().required(),
        description: yup.string().required(),
        quantity: yup.number()
           .min(0, "Quantity must be at least 0")
          .typeError("Quantity must be a number")
          .required(),   
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

  let ticketTypesList =[];
  ticketTypesList.push({id:1, name:"Free", description: "Just free entry and access to watch events", cost: 0});
  ticketTypesList.push({id:2, name:"Basic", description: "Allows one free sample, and access to some premium events", cost: 20});
  ticketTypesList.push({id:3, name:"Advanced", description: "Upto 5 samples, access to premiun seating, meet the performers backstage.", cost: 40});
  

  const dispatch = useAppDispatch();
  const  cart = useAppSelector((state:RootState) => state.cart);
  const navigate = useNavigate();
  const { id } = useParams();
  
  const { control,register, handleSubmit,formState: { errors } } = useForm<TicketFormValues>({
      resolver: yupResolver(schema),
      defaultValues: {
        fullname: cart.fullname,
        email: cart.email,
        tickets: cart.tickets.length>0 ? cart.tickets :
              ticketTypesList.map((t) => ({ id: t.id, name:t.name, quantity: 0 , description: t.description, cost:t.cost})),
      },
    });

  


  const onSubmit = (data: TicketFormValues) => {
    console.log(data);
    dispatch(updatebuyer({ fullname: data.fullname, email: data.email }));
    dispatch(updatetickets({ tickets: data.tickets }));
    navigate(`/ordersummary/${id}`);
  };
return (
   
      <div className="flex flex-col  max-w-xl mx-auto p-4  justify-center">
        <div className="text-3xl font-bold mb-8 text-primary-color text-center">Ticket Types</div>
        {/* <form onSubmit={handleSubmit(
  (data) => console.log("submit fired!", data),
  (errors) => console.log("validation errors", errors)
)}></form> */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {ticketTypesList.map((item, index) => (
                <div key={item.id} className="flex flex-row">
                    <div className="text-l text-secondary-color w-1/2 text-left">{item.name}:  {item.description}</div>
                    <div className="text-xl text-center text-secondary-color  w-1/3">{item.cost}</div>
                    <div className="text-l text-center text-secondary-color">
                        <input
                          key={item.id}
                          type="number"
                          {...register(`tickets.${index}.quantity`, { valueAsNumber: true })}
                          className="w-20 border rounded p-1"
                          min={0}
                        />
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
