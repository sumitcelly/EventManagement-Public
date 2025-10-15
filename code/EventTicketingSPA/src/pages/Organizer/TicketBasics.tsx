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

const ticketSchema = (event: EventHeader)=>yup.object({
  name: yup.string().required("Ticket name is required."),
  cost: yup.number().default(0).required("Ticket cost is required.").min(0, "Ticket cost cannot be negative.") ,
  maxPerOrder: yup.number().default(0).optional(),
  totalAllowed: yup.number().required().min(1, "Total allowed must be at least one."),
  description: yup
    .string()
    .required("Description is required")
    .test("not-empty", "Description is required", (value) => {
  
      const stripped = value?.replace(/<[^>]+>/g, "").trim(); // remove HTML tags
      return !!stripped;
    }),
    tickeSalesStartDate: yup.string().default((new Date()).toLocaleDateString()).required("Ticket sales start date is required")
    .test("past-date", "Ticket sales start date must be before the ends and not in the past.", (value) => {  
           if (!event || !event.eventDate) return false;  
          return  new Date(value) >= new Date() && new Date(value) <= new Date(event.eventDate);      
      }),
    tickeSalesEndDate: yup.string().required("Ticket sales end date is required")
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
  maxPerOrder: number;
  totalAllowed: number;
  description: string; // <-- allow undefined
  tickeSalesStartDate: string;
  tickeSalesEndDate: string;
  tickevalidityStartDate: string;
  tickevalidityEndtDate: string;
};


export default function TicketBasics() {

  const {eventId,ticketId}  = useParams();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  
  //redux may or may not have event details
  let eventBasics = useAppSelector((state:RootState) => state.event);
  
  let { data, isLoading:eventLoading } = useQuery(`events/basics/${eventId}`, async () => {
    const res = await axiosClient.get(`/events/basics/${eventId}`);
    console.log('Event basics from backend', res?.data);
    return res.data;
  },
  {
    //staleTime: 1000 * 60 * 5,
    //enable only if redux does not have event details and there is a valid eventID sent to the page
    enabled: !!eventId && !eventBasics?.eventId
  }
  );


  const { data:ticketDetails, isLoading:ticketLoading } = useQuery(`tickets/details/${eventId}/${ticketId}`, async () => {
    const res = await axiosClient.get(`/eventitemtype/getbyid/${ticketId}`);
    console.log('Event details from backend', res?.data);
    return res.data;
  },
  {
    //staleTime: 1000 * 60 * 5, //enable only if redux does not have event details
    enabled: !!ticketId
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
      totalAllowed: 0,
      description: "",
      tickeSalesStartDate: new Date().toISOString().slice(0,16),
      tickeSalesEndDate: new Date().toISOString().slice(0,16),
      tickevalidityStartDate: new Date().toISOString().slice(0,16),
      tickevalidityEndtDate: new Date().toISOString().slice(0,16),

      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors);  
    }

  function addDurationToDate(date: Date, durationHours: number): Date {
    const newDate = new Date(date);
    newDate.setHours(newDate.getHours() + durationHours);
    return newDate;
  }

  useEffect(() => {
    if (data && !eventLoading) {
      eventBasics = data;
      dispatch(updateEvent({event: eventBasics}));
    }
  }, [data, eventBasics,dispatch]);

  useEffect(() => { 
    if (ticketDetails) { 
      reset(
      {
        name: ticketDetails?.name || "",
        cost: ticketDetails?.cost || 0,
        maxPerOrder: ticketDetails?.maxPerOrder || 0,
        totalAllowed: ticketDetails?.totalAllowed || 0,
        description: ticketDetails?.description || "",
        tickeSalesStartDate: ticketDetails?.tickeSalesStartDate ? new Date(ticketDetails.tickeSalesStartDate).toISOString().slice(0,16) : new Date().toISOString().slice(0,16),
        tickeSalesEndDate: ticketDetails?.tickeSalesEndDate ? new Date(ticketDetails.tickeSalesEndDate).toISOString().slice(0,16) : new Date(eventBasics.eventDate).toISOString().slice(0,16),
        tickevalidityStartDate: ticketDetails?.tickevalidityStartDate ? new Date(ticketDetails.tickevalidityStartDate).toISOString().slice(0,16) : new Date(eventBasics.eventDate).toISOString().slice(0,16),
        tickevalidityEndtDate: ticketDetails?.tickevalidityEndtDate
                              ?new Date(ticketDetails.tickevalidityEndtDate).toISOString().slice(0,16) 
                              : addDurationToDate(new Date(eventBasics.eventDate), eventBasics.duration || 0).toISOString().slice(0,16), });
      
  }
  }, [ticketDetails, reset]);

  if (ticketLoading || eventLoading) return <p>Loading...</p>;

  return (
    <form
      onSubmit={handleSubmit(
        //console.log("address", fullAddress),
  (data) => console.log("submit fired!", data),
  (errors) => console.log("validation errors", errors)
)}>
    {/* // <form onSubmit={handleSubmit(onSubmit)}
    //   className="max-w-2xl mx-auto p-6 space-y-6"
    // >  */}
      <div>
        <label className="block font-semibold mb-1">Ticket Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-full border rounded p-2"
          placeholder="Enter event title"
        />
        {errors.name && (
          <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
        )}
      </div>
      <div>
        <label className="block font-semibold mb-1">Description</label>
        <Controller
          name="description"
          control={control}
          render={({ field }) => (
            <RichTextEditor value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.description && (
          <p className="text-red-600 text-sm mt-1">
            {errors.description.message}
          </p>
        )}
      </div>

      <div>
        <label className="block font-semibold mb-1">Cost</label>
        <input
          type="number"
          {...register("cost")}
          className="w-full border rounded p-2"
          placeholder="Ticket cost($)"
        />
        {errors.name && (
          <p className="text-red-600 text-sm mt-1">{errors.cost?.message}</p>
        )}
      </div>

      <div className="flex flex-row items-center justify-between">
        <div className="flex flex-col items-end">
          <label className="font-semibold mb-1">Ticket Limit</label>
          <input 
            type="number"
            {...register("totalAllowed")}
            className="ml-auto w-1/4 border rounded p-2"
            placeholder="Number of tickets"
            min={0}
          />
          {errors.totalAllowed && (
            <p className="text-red-600 text-sm mt-1">
              {errors.totalAllowed.message}
            </p>
          )}
        </div>
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Maximum per order</label>
          <input
            type="number"
            {...register("maxPerOrder")}
            className="border rounded p-2"
            placeholder="Max per order (0 for no limit)"
            min={0}
          />
          {errors.maxPerOrder && (
            <p className="text-red-600 text-sm mt-1">
              {errors.maxPerOrder.message}
            </p>
          )}
        </div>   
      </div>
       <div className="flex flex-row items-center justify-between">
        <div className="flex flex-col items-end">
          <label className="font-semibold mb-1">Sale start date</label>
          <input 
            type="datetime-local"
            {...register("tickeSalesStartDate")}
            className="ml-auto w-1/4 border rounded p-2"
          
          />
          {errors.tickeSalesStartDate && (
            <p className="text-red-600 text-sm mt-1">
              {errors.tickeSalesStartDate.message}
            </p>
          )}
        </div>
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Sale end date</label>
          <input
            type="datetime-local"
            {...register("tickeSalesEndDate")}
            className="border rounded p-2"
          
          />
          {errors.tickeSalesEndDate && (
            <p className="text-red-600 text-sm mt-1">
              {errors.tickeSalesEndDate.message}
            </p>
          )}
        </div>   
      </div>

     

      <button
        type="submit"
        className="ml-auto bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
      >
        Submit
      </button>
    </form>
  );
}
