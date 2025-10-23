// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";

import axiosClient from "../../api/axiosClient";
import { useQuery } from "react-query";
import { useParams, useNavigate } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import toast, { Toaster } from 'react-hot-toast';

const ticketSchema = (event: EventHeader)=>yup.object({
  name: yup.string().required("Ticket name is required.").typeError('Invalid number.'),
  cost: yup.number().default(0).required("Ticket cost is required.").
        min(0, "Ticket cost cannot be negative.")
        .typeError('Invalid number.'),
  maxPerOrder: yup.number().default(0).nullable().typeError('Invalid number.'),
  totalAllowed: yup.number().default(1).required().min(1, "Total allowed must be at least one.").typeError('Invalid number.'),
  description: yup
    .string()
    .required("Description is required")
    .test("not-empty", "Description is required", (value) => {
  
      const stripped = value?.replace(/<[^>]+>/g, "").trim(); // remove HTML tags
      return !!stripped;
    }),
    tickeSalesStartDate: yup.string().default(new Date().toLocaleDateString()).required("Ticket sales start date is required")
    .test("past-date", "Either ticket sales date is in the past or after event has ended.", (value) => {  
           if (!event || !event.eventDate) return false;  
          return  new Date(value) >= new Date() && new Date(value) <= new Date(event.eventDate);      
      }),
    tickeSalesEndDate: yup.string().default(event?.eventDate? new Date(event.eventDate).toLocaleDateString(): (new Date()).toLocaleDateString())
                      .required("Ticket sales end date is required")
    .test("ticket-sale-end-date", "Ticket sales end date cannot be after the event has ended.", (value) => {  
        if (!event || !event.eventDate) return false;
        return  new Date(value) <= new Date(event.eventDate);      
      })
    .test("end-after-start", "Ticket sales end date must be after start date", function(value) {
        const { tickeSalesStartDate } = this.parent;
        return new Date(value) > new Date(tickeSalesStartDate);
      }),
    tickevalidityStartDate: yup.string().required("Ticket validity start date is required")
    .test("past-date", "Tickets must be valid during the course of the event.", (value) => {  
        
        if (!event || !event.eventDate) return false;
        const newDate = new Date(event.eventDate); 
        newDate.setHours(newDate.getHours() + (event.duration || 0));
        return  (new Date(value) >= new Date(event.eventDate)) && (new Date(value) <= newDate);     
      }),
    tickevalidityEndtDate: yup.string().required("Ticket validity end date is required")
    .test("past-date", "Tickets must be valid during the course of the event.", (value) => {  
        if (!event || !event.eventDate) return false;
        const newDate = new Date(event.eventDate); 
        newDate.setHours(newDate.getHours() + (event.duration || 0));
        return  (new Date(value) >= new Date(event.eventDate)) && (new Date(value) <= newDate);     
      }),
  });

type FormValues = {
  name: string;
  cost: number;
  maxPerOrder: number | null;
  totalAllowed: number;
  description: string; // <-- allow undefined
  tickeSalesStartDate: string;
  tickeSalesEndDate: string;
  tickevalidityStartDate: string;
  tickevalidityEndtDate: string;
};

const toLocalDateTimeInputValue = (date?: Date | string | null) => {

  //console.log(date,"the date passed");
  if (!date) {
    console.warn("⚠️ No date passed to toLocalDateTimeInputValue");
    return "";
  }

  const d = new Date(date);
  if (isNaN(d.getTime())) {
    //console.log("⚠️ Invalid date passed:", d);
    return "";
  }

  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
};


  const toUTCDate = (localDateStr: string): Date => {
    const localDate = new Date(localDateStr);
    return new Date(localDate.getTime() + localDate.getTimezoneOffset() * 60000); 
  }
  function addDurationToDate(date: Date, durationHours: number): Date {
    const newDate = new Date(date);
    newDate.setHours(newDate.getHours() + durationHours);
    return newDate;
  }


export default function TicketBasics({eventId,ticketId}: {eventId?: string, ticketId?:string}) {

  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  
  //redux may or may not have event details
  let eventBasics = useAppSelector((state:RootState) => state.event);
  
  let { data, isLoading:eventLoading } = useQuery(`events/basics/${eventId}`, async () => {
    const res = await axiosClient.get(`/events/basics/${eventId}`);
    console.log('Event basics from backend', res?.data);
    eventBasics = res.data;
    dispatch(updateEvent({event: eventBasics}));
    return res.data;
  },
  {
    //staleTime: 1000 * 60 * 5,
    //enable only if redux does not have event details and there is a valid eventID sent to the page
    enabled: !!eventId && !eventBasics?.eventId
  }
  );


  const { data:ticketDetails, isLoading:ticketLoading } = useQuery(`tickets/details/${eventId}/${ticketId}`, async () => {
    const res = await axiosClient.get(`/eventitemtype/${ticketId}`);
    console.log('ticket details from backend', res?.data);
    return res.data;
  },
  {
    //staleTime: 1000 * 60 * 5, //enable only if redux does not have event details
    enabled: !!ticketId && ticketId!=="-1"
  }
  );


  const {
    control,
    setValue,
    handleSubmit,
    register,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(ticketSchema(eventBasics)),
    defaultValues: {
      name: "",
      cost: 0,
      maxPerOrder: 0,
      totalAllowed: 1 ,
      description: "",
      tickeSalesStartDate: toLocalDateTimeInputValue(addDurationToDate(new Date(),1)), //default to one hour from now
      tickeSalesEndDate: toLocalDateTimeInputValue(eventBasics.eventDate ? (eventBasics.eventDate) : addDurationToDate(new Date(),24)),
      tickevalidityStartDate: toLocalDateTimeInputValue(eventBasics.eventDate ? new Date(eventBasics.eventDate) : new Date()),
      tickevalidityEndtDate: toLocalDateTimeInputValue(eventBasics.eventDate ? addDurationToDate(new Date(eventBasics.eventDate), eventBasics.duration ||0) : new Date()),

      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    //make sure to convert date time local to UTC before sending to backend
    const payload = {
      eventItemTypeId: ticketDetails?.eventItemTypeId || 0,
      eventId: Number(eventId),
      name: data.name,
      description: data.description,
      cost: data.cost,
      maxPerOrder: data.maxPerOrder,
      totalAllowed: data.totalAllowed,
      tickeSalesStartDate: toUTCDate(data.tickeSalesStartDate),
      tickeSalesEndDate: toUTCDate(data.tickeSalesEndDate),
      tickevalidityStartDate: toUTCDate(data.tickevalidityStartDate),
      tickevalidityEndtDate: toUTCDate(data.tickevalidityEndtDate),
    }
    console.log('payload to be sent to backend',payload);
    if (!user || !user.id) {
      console.error("User not authenticated");
      return;
    }
    if (ticketDetails) {
      //update
        toast.success("Ticket updated!");
        setTimeout(()=> navigate(`/eventmanager/${eventId}/ticketlist`),1500);
      // axiosClient.put(`/eventitemtype/update/${ticketDetails.eventItemTypeId}`, payload)
      // .then(response => {
      //   console.log('Ticket updated successfully:', response.data);
      //   navigate(`/organizer/eventtickets/${eventId}`);
      // })
      // .catch(error => {
      //   console.error('Error updating ticket:', error);
      // });
    }
    else {
      toast.success("Ticket created!");
      setTimeout(()=> navigate(`/eventmanager/${eventId}/ticketlist`),1500);
      //create
      // axiosClient.post(`/eventitemtype/create`, payload)
      // .then(response => {
      //   console.log('Ticket created successfully:', response.data);
      //   navigate(`/organizer/ticketdashboard/${eventId}`);
      // })
      // .catch(error => {
      //   console.error('Error creating ticket:', error);
      // });
    }
  }



  useEffect(() => { 
    if (ticketDetails) { 
      console.log("Inside reset",ticketDetails);
      reset(
      {
        name: ticketDetails?.name || "",
        cost: ticketDetails?.cost || 0,
        maxPerOrder: ticketDetails?.maxPerOrder || 0,
        totalAllowed: ticketDetails?.totalAllowed || 0,
        description: ticketDetails?.description || "",
        tickeSalesStartDate: ticketDetails?.tickeSalesStartDate ? toLocalDateTimeInputValue(new Date(ticketDetails.tickeSalesStartDate)) : toLocalDateTimeInputValue(addDurationToDate(new Date(),1)),
        tickeSalesEndDate: ticketDetails?.tickeSalesEndDate? toLocalDateTimeInputValue(new Date(ticketDetails.tickeSalesEndDate)) : toLocalDateTimeInputValue(addDurationToDate(new Date(),24)),
        tickevalidityStartDate: ticketDetails?.tickevalidityStartDate ? toLocalDateTimeInputValue(new Date(ticketDetails.tickevalidityStartDate)) 
                              : toLocalDateTimeInputValue(new Date(eventBasics.eventDate)),
        tickevalidityEndtDate: ticketDetails?.tickevalidityEndtDate
                              ? toLocalDateTimeInputValue(new Date(ticketDetails.tickevalidityEndtDate))
                              : toLocalDateTimeInputValue(addDurationToDate(new Date(eventBasics.eventDate), eventBasics.duration || 0))

      });
    }
  }, [ticketDetails, reset, eventBasics]);

  if (ticketLoading || eventLoading) return <p>Loading...</p>;

  return (
    
    // <form
    // className="max-w-2xl mx-auto p-6 space-y-6"
    //   onSubmit={handleSubmit(
    //     //console.log("address", fullAddress),
    //   (data) => console.log("submit fired!", data),
    //   (errors) => console.log("validation errors", errors)
    // )}>
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-2xl mx-auto p-6 space-y-6"
    > 
     <Toaster position="top-right" />
      <a href={`/eventmanager/${eventId}/ticketlist`} className="mr-auto text-accent-color">
          Back to Ticket list
      </a>
     
      <div  className="min-h-[80px]">
        <label className="block font-semibold mb-1">Ticket Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-full border rounded p-2"
          placeholder="Enter ticket name"
        />
        <div className="h-5">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
          )}
        </div>
      </div>
      
      <div className="flex flex-col w-1/5 min-h-[80px]">
        <label className="font-semibold mb-1">Cost($)</label>
        <input
          type="number"
          {...register("cost")}
          className="border rounded p-2"
          min={0}
        />
        {errors.name && (
          <p className="text-red-600 text-sm mt-1">{errors.cost?.message}</p>
        )}
      </div>

      <div className= "overflow-y-auto min-h-[150px] max-h-[300px]">
        <label className="block font-semibold mb-1">Description</label>
        <Controller
          name="description"
          control={control}
          render={({ field }) => (
            <RichTextEditor value={field.value} onChange={field.onChange} />
          )}
        />
        <div className="h-5">
          {errors.description && (
            <p className="text-red-600 text-sm mt-1">
              {errors.description.message}
            </p>
          )}
        </div>
      </div>

    
      <div className="flex flex-row items-center justify-between min-h-[80px]">
        <div className="flex flex-col w-1/5">
          <label className="font-semibold mb-1">Ticket Quantity</label>
          <input 
            type="number"
            {...register("totalAllowed")}
            className="border rounded p-2"
            min={0}
            title="Total number of tickets that can be sold for this ticket type."
          />
          <div className="h-5">
            {errors.totalAllowed && (
              <p className="text-red-600 text-sm mt-1">
                {errors.totalAllowed.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col w-1/4">
          <label className="font-semibold mb-1">Per order limit</label>
          <input
            type="number"
            {...register("maxPerOrder")}
            className="border rounded p-2"
            min={0}
            title="Maximum number of tickets of this type that can be purchased in a single order. 0 means no limit."
          />
          <div className="h-5">
            {errors.maxPerOrder && (
              <p className="text-red-600 text-sm mt-1">
                {errors.maxPerOrder.message}
              </p>
            )}
          </div>   
        </div>
      </div>
      
       <div className="flex flex-row items-center justify-between min-h-[80px]">
        <div className="flex flex-col  justify-between">
          <label className="font-semibold mb-1">Sale start date</label>
          <div>
            <input 
              type="datetime-local"
              {...register("tickeSalesStartDate")}
              className="border rounded p-2"
            
            />
          </div>
          <div className="h-5">
            {errors.tickeSalesStartDate && (
              <p className="text-red-600 text-sm mt-1">
                {errors.tickeSalesStartDate.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Sale end date</label>
          <input
            type="datetime-local"
            {...register("tickeSalesEndDate")}
            className="border rounded p-2"
          
          />
          <div className="h-5">
            {errors.tickeSalesEndDate && (
              <p className="text-red-600 text-sm mt-1 wrap">
                {errors.tickeSalesEndDate.message}
              </p>
            )}
          </div>   
        </div>  
      </div>
      
      <div className="flex flex-row items-center justify-between min-h-[80px]">
        <div className="flex flex-col justify-between">
          <label className="font-semibold mb-1">Validity start date</label>
          <div>
            <input 
              type="datetime-local"
              {...register("tickevalidityStartDate")}
              className="border rounded p-2"
            />
          </div>
          <div className="h-5">
            {errors.tickevalidityStartDate && (
              <p className="text-red-600 text-sm mt-1">
                {errors.tickevalidityStartDate.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Validity end date</label>
          <div>
            <input
              type="datetime-local"
              {...register("tickevalidityEndtDate")}
              className="border rounded p-2"  
            />
          </div>
          <div className="h-5">
            {errors.tickevalidityEndtDate && (
              <p className="text-red-600 text-sm mt-1 wrap">
                {errors.tickevalidityEndtDate.message}
              </p>
            )}
          </div>   
        </div>  
      </div>

     
      <div className="flex flex-row items-center justify-between">
        <button
          type="submit"
          className="ml-auto bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </form>
  );
}
