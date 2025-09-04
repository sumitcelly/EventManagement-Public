// src/pages/Login.jsx
import { useForm, Controller } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useAppDispatch ,useAppSelector} from "../app/hook";

interface CheckoutFormInputs {
  email: string;
  fullname: string;
  quantity:number[];
}

interface TicketTypes
{
  id: number;
  name: string;
  description: string;
  cost: number;
}


const schema = yup.object({
  email: yup.string().required("Email is required"),
  fullname: yup.string().required("Fullname is required"),
  tickets: yup
    .array()
    .of(
    yup.object({
      type: yup.number().required(),
      quantity: yup
        .number()
        .typeError("Quantity must be a number")
        .min(0, "Quantity must be at least 0")
        .required(),   
      })
      )
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
  const { status, error } = useAppSelector((state) => state.auth);

  const { control, register,handleSubmit,formState: { errors } } = useForm({
      resolver: yupResolver(schema),
      defaultValues: {
        tickets: ticketTypesList.map((t) => ({ type: t.id, quantity: 0 })),
      },
    });

  const onSubmit = (data :any) => {
    //dispatch(loginUser(data));
  };

  return (
    <div className="flex flex-row  justifiy-center max-w-3xl mx-auto mt-3 p-6 bg-white shadow rounded h-view">
      <div className="flex flex-col w-2/3 p-4 border-r border-gray-300 justify-center">
        <div className="text-3xl font-bold mb-8 text-primary-color text-center">Ticket Types</div>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {ticketTypesList.map((item:TicketTypes, index) => (
                <div key={item.id} className="flex flex-row">
                    <div className="text-l text-secondary-color w-1/2 text-left">{item.name}  {item.description}</div>
                    <div className="text-xl text-center text-secondary-color  w-1/3">{item.cost}</div>
                    <div className="text-l text-center text-secondary-color">

                     <Controller
                        
                          name={`tickets.${index}.quantity`}
                          control={control}
                          render={({ field, fieldState }) => (
                            <div>
                              <input
                                type="number"
                                {...field}
                                className="w-20 border rounded p-1"
                                min={0}
                              />
                             
                            </div>
                          )}
                        />
                    </div>
                    
                </div>
              ))}

               {errors.tickets && (
                  <p className="text-red-500 text-sm mt-2">
                     {errors.tickets.message || errors.tickets.root?.message}
                  </p>
                )}


             <div className="md:w-1/2 ml-auto align-left flex flex-col p-4 border-gray-300 justify-center">
              <label className="text-sm font-medium">Full Name</label>
                <input
                  type="text"
                  {...register("fullname")}
                  className="border rounded px-3 py-2"
                />
                {errors.email && (
                  <p className="text-red-500 text-sm">{errors.email.message}</p>
                )}
       

            {/* Password */}
            
                <label className="mt-3 block text-sm font-medium">Email</label>
                <input
                  type="text"
                  {...register("email")}
                  className="border rounded px-3 py-2"
                />
                {errors.email && (
                  <p className="text-red-500 text-sm">{errors.email.message}</p>
                )}

                <button
                    type="submit"
                    disabled={status === "loading"}
                    className="mt-3 w-12/ ml-auto bg-brand-dark text-white px-4 py-2 rounded hover:bg-blue-700 
                    disabled:bg-gray-400"
                  >
                    {status === "loading" ? "Logging in..." : "Checkout"}
                </button>
            </div>
           
        </form>
      </div>

      <div className="flex flex-col w-1/3 p-4 border-r border-gray-300 ml-4">
        <div className="text-2xl font-bold mb-4 text-primary-color">Order Total</div>
      </div>
    </div>
  );
}
