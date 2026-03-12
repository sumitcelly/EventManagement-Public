// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { useParams, useHistory } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import toast, { Toaster } from 'react-hot-toast';
import {appendTime,toUTCDate, addHoursToDate,combineDateTime}   from '../../utils/DateUtils'
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";


const ticketSchema = (event: EventHeader)=>yup.object({
  name: yup.string().required("Ticket name is required.").typeError('Invalid number.'),
  cost: yup.number().default(0).required("Ticket cost is required.").
        min(0, "Ticket cost cannot be negative.")
        .typeError('Invalid number.'),
  maxPerOrder: yup.number().default(0).nullable().typeError('Invalid number.'),
  totalAllowed: yup.number().default(0).required().min(1, "Total allowed must be at least one.").typeError('Invalid number.'),
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
          //console.log("value",parseDateOnlyString(value),new Date(),parseDateOnlyString(value) >= new Date());
          return  appendTime(value,true) >= new Date() 
           &&  appendTime(value,true) <= new Date(event.eventDate);      
      }),
    tickeSalesEndDate: yup.string().default(event?.eventDate? new Date(event.eventDate).toLocaleDateString(): (new Date()).toLocaleDateString())
                      .required("Ticket sales end date is required")
    .test("ticket-sale-end-date", "Ticket sales end date cannot be after the event has ended.", (value) => {  
        if (!event || !event.eventDate) return false;
        return  appendTime(value) <= new Date(event.eventDate);      
      })
    .test("end-after-start", "Ticket sales end date must be after start date", function(value) {
        const { tickeSalesStartDate } = this.parent;
        return new Date(value) > new Date(tickeSalesStartDate);
      }),
    tickevalidityStartDate: yup.string().required("Ticket validity start date is required")
    .test("past-date", "Tickets must be valid during the course of the event.", (value) => {  
        console.log(value);
        //return true;
        if (!event || !event.eventDate) return false;
      
        const  validityDate =  new Date(combineDateTime( new Date(event.eventDate),value));
        const eventEndDate = addHoursToDate(event.eventDate, event.duration ||0);
        console.log('validityDate, eventEndDate', validityDate, eventEndDate);
        return  validityDate >= event.eventDate && validityDate<=eventEndDate;     
      }),
    tickevalidityEndtDate: yup.string().required("Ticket validity end date is required")
    .test("past-date", "Tickets must be valid during the course of the event.", (value) => { 
      
        if (!event || !event.eventDate) return false;
        const  validityDate =  new Date(combineDateTime( new Date(event.eventDate),value));
        const eventEndDate = addHoursToDate(event.eventDate, event.duration ||0);
        return  validityDate >= event.eventDate && validityDate<=eventEndDate;      
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




export default function TicketBasics( {eventId,ticketId,mode}:
   {eventId?: string, ticketId?:string,mode?:string})
 {

  const history = useHistory();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  const queryClient =useQueryClient();
  const stripeConnectStatus =  user?.stripeConnectStatus;
 

  console.log('ticket basics eventid, ticketid, mode',eventId,ticketId,mode);

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
    staleTime: 1000 * 60 * 5,
    //enable only if redux does not have event details and there is a valid eventID sent to the page
    enabled: !!eventId && !eventBasics?.eventId
  }
  );


  const { data:ticketDetails, isLoading:ticketLoading } = useQuery(['TicketDetails',eventId,ticketId], async () => {
    
    console.log('ticket id from query',ticketId);
  
    let res = await axiosClient.get(`/eventitemtype/${eventId}/${ticketId}`);
    if (res.data)
    {
      res.data.salesStartDate+="Z";
      res.data.salesEndDate+="Z";
      res.data.ticketValidityStart+="Z";
      res.data.ticketValidityEnd+="Z";
    }
    console.log('ticket details from backend', res?.data);
    return res.data;
  },
  {
    //refetchOnMount:true,
    //refetchOnWindowFocus:true,
    
    staleTime: 1000 * 60 * 5, //enable only if redux does not have event details
    enabled: !!ticketId && ticketId!=="-1"
  }
  );

  console.log('ticket details',ticketDetails);


  const getEventEndTime =():string=>{
    let retVal = "";

    if (eventBasics?.duration && eventBasics?.eventDate)
    {
      const eventEnd = addHoursToDate(eventBasics.eventDate, eventBasics.duration);

      retVal=`T${eventEnd.getHours()}:00:00.000`;
      console.log('enddate',retVal);
    }
    return retVal;
    
  }

  const setDefaultFormValues =()=>{
    return {
      name: "",
      cost: 0,
      maxPerOrder: 0,
      totalAllowed: 1 ,
      description: "",
      tickeSalesStartDate: new Date().toLocaleDateString('sv-SE').split('T')[0],
      tickeSalesEndDate: eventBasics.eventDate ? new Date(eventBasics.eventDate).toLocaleDateString('sv-SE').split('T')[0] : new Date().toISOString().split('T')[0],
      tickevalidityStartDate: eventBasics.eventDate ? 
                             new Date(eventBasics.eventDate).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                            '00:00',
      tickevalidityEndtDate: eventBasics.eventDate && eventBasics.duration ? 
                            addHoursToDate(new Date(eventBasics.eventDate), eventBasics.duration).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                            '23:59',

      };
  }


  const {
    control,
    setValue,
    setError,
    handleSubmit,
    register,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(ticketSchema(eventBasics)),
    defaultValues:  {
      name: "",
      cost: 0,
      maxPerOrder: 0,
      totalAllowed: 1 ,
      description: "",
      tickeSalesStartDate: new Date().toLocaleDateString('sv-SE').split('T')[0],
      tickeSalesEndDate: eventBasics.eventDate ? new Date(eventBasics.eventDate).toLocaleDateString('sv-SE').split('T')[0] : new Date().toISOString().split('T')[0],
      tickevalidityStartDate: eventBasics.eventDate ? 
                             new Date(eventBasics.eventDate).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                            '00:00',
      tickevalidityEndtDate: eventBasics.eventDate && eventBasics.duration ? 
                            addHoursToDate(new Date(eventBasics.eventDate), eventBasics.duration).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                            '23:59',

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
      salesStartDate: new Date(data.tickeSalesStartDate + 'T00:00:00.000').toISOString(),
      salesEndDate: new Date(data.tickeSalesEndDate + getEventEndTime()).toISOString(),
      ticketValidityStart: combineDateTime(eventBasics.eventDate, data.tickevalidityStartDate),
      ticketValidityEnd: combineDateTime(eventBasics.eventDate, data.tickevalidityEndtDate),
    }
    console.log('payload to be sent to backend',payload);
   

    if (!user || !user.id) {
      console.error("User not authenticated");
      return;
    }
    if (ticketDetails) {
      //update     
       
        axiosClient.put(`/eventitemtype/${eventId}/${ticketDetails.eventItemTypeId}`, payload)
        .then(response => {
          toast.success("Ticket type updated!");
          console.log('Ticket updated successfully:', response.data);
          queryClient.invalidateQueries(['TicketsbyEvent', eventId]);
          queryClient.invalidateQueries(['TicketDetails',eventId,ticketId]);
          //queryClient.resetQueries({queryKey:[`tickets/details/${eventId}/${ticketId}`]});
          //setTimeout(()=> navigate(`/eventmanager/${eventId}/ticketlist`),1500);
          //navigate(`/organizer/eventtickets/${eventId}`);
        })
        .catch(error => {
            toast.error("Ticket type updation failed!");
            console.error('Error updating ticket:', error);
        });
    }
    else {
        //create
      axiosClient.post(`/eventitemtype/${eventId}`, payload)
      .then(response => {
        console.log('Ticket created successfully:', response.data);
        toast.success("Ticket Type created!");
        queryClient.invalidateQueries(['TicketsbyEvent', eventId]);
        //queryClient.s(['TicketDetails',eventId,response.data]);
        setTimeout(()=> history.push(`/eventmanager/${eventId}/ticketlist`),1500);
      })
      .catch(error => {
         toast.error("Ticket Type creation failed!");
        console.error('Error creating ticket:', error);
      });

    }
  }



  useEffect(() => { 
    if (ticketDetails)
    { 
      console.log("Inside reset",ticketDetails);
      reset(
      {
        name: ticketDetails?.name || "",
        cost: ticketDetails?.cost || 0,
        maxPerOrder: ticketDetails?.maxPerOrder || 0,
        totalAllowed: ticketDetails?.totalAllowed || 0,
        description: ticketDetails?.description || "",
        tickeSalesStartDate: ticketDetails?.salesStartDate ? new Date(ticketDetails.salesStartDate).toLocaleDateString('sv-SE').split('T')[0] : 
                            new Date().toLocaleDateString('sv-SE').split('T')[0],
        tickeSalesEndDate: ticketDetails?.salesEndDate ? new Date(ticketDetails.salesEndDate).toLocaleDateString('sv-SE').split('T')[0] :
                             new Date(eventBasics.eventDate).toLocaleDateString('sv-SE').split('T')[0],
        tickevalidityStartDate: ticketDetails?.ticketValidityStart ? new Date(ticketDetails.ticketValidityStart).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                                new Date(eventBasics?.eventDate).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) ,
        tickevalidityEndtDate: ticketDetails?.ticketValidityEnd ? new Date(ticketDetails.ticketValidityEnd).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) : 
                                addHoursToDate(new Date(eventBasics.eventDate), eventBasics.duration || 0).toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit' }) ,      
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
      className="max-w-2xl mx-auto p-3 space-y-2"
    > 
     {/* <Toaster position="top-right" /> */}
      <a href={`/eventmanager/${eventId}/ticketlist`} className="mr-auto text-link-color" 
        onClick={(e)=>{
          e.preventDefault();
          history.push(`/eventmanager/${eventId}/ticketlist`);
      }}>
          Back to Ticket list
      </a>
     
      <div className="flex flex-col">
        <label className="block font-semibold mb-1">Ticket Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-full border rounded p-2"
          placeholder="Enter ticket name"
        />
         <div className="min-h-[20px]">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
          )}
        </div>
      </div>
      
      <div className="flex flex-row justify-items-center justify-between">
        <div className="flex flex-col w-1/5">
          <label className="font-semibold mb-1">Cost($)</label>
          {/* <div className="flex flex-row justify-between items-center"> */}
            <input
              type="number"
              step={.01}
              {...register("cost")}
              className="border rounded p-2"
              min={0}
              placeholder="0.00"
              disabled={stripeConnectStatus !== "Completed"}
            />
            
            {stripeConnectStatus !=="Completed" && (<div className="text-accent-color mr-auto max-w-[250px]">
              You must be connected to stripe in order to host paid events!
          </div>)} 
          {/* </div> */}
          <div className="min-h-[20px]">
            {errors.name && (
              <p className="text-red-600 text-sm mt-1">{errors.cost?.message}</p>
            )}
          </div>
        </div>
        <div className="flex flex-col w-1/5">
          <label className="font-semibold mb-1">Ticket Quantity</label>
          <input 
            type="number"
            {...register("totalAllowed")}
            className="border rounded p-2"
            min={0}
            title="Total number of tickets that can be sold for this ticket type."
          />
          <div className="min-h-[20px]">
            {errors.totalAllowed && (
              <p className="text-red-600 text-sm mt-1">
                {errors.totalAllowed.message}
              </p>
            )}
          </div>
        </div>
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
         <div className="min-h-[20px]">
          {errors.description && (
            <p className="text-red-600 text-sm mt-1">
              {errors.description.message}
            </p>
          )}
        </div>
      </div>

    
    <details>
      <summary>Click to view advanced ticket settings</summary>
      <div className="flex flex-col w-1/4 ml-auto">
        <label className="font-semibold mb-1">Per order limit</label>
        <input
          type="number"
          {...register("maxPerOrder")}
          className="border rounded p-2"
          min={0}
          title="Maximum number of tickets of this type that can be purchased in a single order. 0 means no limit."
        />
        <div className="min-h-[20px]">
          {errors.maxPerOrder && (
            <p className="text-red-600 text-sm mt-1">
              {errors.maxPerOrder.message}
            </p>
          )}
        </div>   
      </div>
    
      
       <div className="flex flex-row items-center justify-between">
        <div className="flex flex-col  justify-between">
          <label className="font-semibold mb-1">Sale start date</label>
          <div>
            <input 
              type="date"
              {...register("tickeSalesStartDate")}
              className="border rounded p-2"
               title={`Event is on ${eventBasics.eventDate.toDateString()} `}
            
            />
          </div>
          <div className="min-h-[20px] max-w-[150px]">
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
            type="date"
            {...register("tickeSalesEndDate")}
            className="border rounded p-2"
            title={`Event is on ${eventBasics.eventDate.toDateString()} `}
          />
          <div className="min-h-[20px] max-w-[150px]">
            {errors.tickeSalesEndDate && (
              <p className="text-red-600 text-sm mt-1 wrap">
                {errors.tickeSalesEndDate.message}
              </p>
            )}
          </div>   
        </div>  
      </div>
      
      <details>
        <summary>Use this section to restrict tickets to a certain timeframe within an event.
        </summary>
        <div className="flex flex-row items-center justify-between">
        <div className="flex flex-col justify-between">
          <label className="font-semibold mb-1">Ticket valid from</label>
          <div>
            <input 
              type="time"
              {...register("tickevalidityStartDate")}
              className="border rounded p-2"
              title = {`Event starts at ${eventBasics.eventDate.toLocaleTimeString('en-US',
                { hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                  })}`}
            />
          </div>
          <div className="min-h-[20px] max-w-[150px]">
            {errors.tickevalidityStartDate && (
              <p className="text-red-600 text-sm mt-1">
                {errors.tickevalidityStartDate.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Ticket valid to</label>
          <div>
            <input
              type="time"
              title={`Event ends at ${ addHoursToDate(eventBasics.eventDate, eventBasics.duration|| 0).toLocaleTimeString('en-US', {
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                  })}`}
              {...register("tickevalidityEndtDate")}
              className="border rounded p-2"  
            />
          </div>
          <div className="min-h-[20px] max-w-[150px]">
            {errors.tickevalidityEndtDate && (
              <p className="text-red-600 text-sm mt-1 wrap">
                {errors.tickevalidityEndtDate.message}
              </p>
            )}
          </div>   
        </div>  
      </div>
      </details>
      
      </details>

     
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
