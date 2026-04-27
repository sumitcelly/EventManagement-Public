// EventForm.tsx
import React, { use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";
import FileUpload from "../../components/FileUpload";
import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { useParams, useHistory } from "react-router-dom";
import MapboxAddressField, { AddressData } from "../../components/MapboxAddressField";
import ListInput from "../../components/ListInput";
import { useAppDispatch, useAppSelector } from "../../app/hook";
import { resetEvent, updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
//import  SuccessToast  from "../../components/SuccessToast";
import toast, { Toaster } from 'react-hot-toast';
import axios from "axios";
import { createUrlSlug, getFullUrlForEvent } from "../../utils/StringUtils";
import { addHoursToDate } from "../../utils/DateUtils";
import { TicketFeeMode } from "../../types/Event";
const eventSchema = yup.object({
  eventName: yup.string().required("Event name is required"),
  eventUrlName: yup.string().required("Event Url name is reqired"),
  headline: yup.string().nullable().default(null),
  eventStartDate: yup.string().required("Event date is required")
  .test("past-date", "Event start date cannot be in the past", (value) => {
  
      return  new Date(value) >= new Date();
      
    }),
  eventCategory: yup.string().required("Event category is required"),
  fullAddress: yup.string().required("Event address is required"),
  eventDuration: yup.number().required("Event duration is required").
  min(1, "Duration cannot be 0.")
  .test("next-day", "Your event must end on the same day.", function(value) {
     const { eventStartDate } = this.parent;
  
      // 1. Validation guard: if values are missing, don't fail yet
      if (!eventStartDate || !value) return true;

      const start = new Date(eventStartDate);
      const end = addHoursToDate(start, value);

      // 2. Compare using toDateString (ignores time, compares Day Month Year)
      const isSameDay = start.toDateString() === end.toDateString();

      console.log('Start:', start.toDateString(), 'End:', end.toDateString());

      // 3. Return the boolean result
      return isSameDay; 
      }),
  description: yup
    .string()
    .test("not-empty", "Description is required", (value) => {
      const stripped = value?.replace(/<[^>]+>/g, "").trim(); // remove HTML tags
      return !!stripped;
    })
    .required("Description is required"),
    //important to allow default (null) here if we want to allow null values. This ensures field is never undefined
   agenda: yup.string().nullable().default(null) // <-- allow null or undefined,
});

type FormValues = {
  eventName: string;
  eventUrlName: string;
  eventStartDate: string;
  eventDuration: number;
  eventCategory: string; 
  description: string;
  headline: string | null; // <-- allow undefined
  tagList?: string[];
  agenda: string | null; // <-- allow undefined
  fullAddress: string;
  street?: string;
  city?: string;
  state?: string;
  zip?: string;
  lat?: number;
  lng?: number;
};


export default function EventForm({id,organizerEventBaseUrl, isActive}: {id?: string,organizerEventBaseUrl:string,isActive:boolean}) {

  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [file, setFile] = useState<File | null>(null);

  const queryClient = useQueryClient();

  useEffect(() => {
    if (imagePreview) {
      console.log("Image preview available:", imagePreview)
    }
  }, [imagePreview]) // 

  const history = useHistory();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  //const event = useAppSelector((state: RootState) => state.event);
  
 

  const { data:eventDetails, isLoading } = useQuery(`events/details/${id}`, async () => {
    let res = await axiosClient.get(`/events/details/${id}`);
    console.log('Event details from backend', res?.data);
    //not sure why when a new event is saved, the date returned does not have a Z at the end.
    //when I modify the date, it has that. no idea.
    let dateTemp = !res?.data?.eventDate.endsWith("Z")? res?.data?.eventDate+ "Z":res?.data?.eventDate;
    console.log("date temp", dateTemp);
    if (dateTemp)
    {
      res.data.eventDate = dateTemp;
    }
    const eventBasicInfo = {
      eventId: res.data.eventId,
      eventName: res.data.eventName,
      eventHeadline: res.data.headline,
      eventDate: new Date(dateTemp),
      duration: res.data.duration,
      eventLocation: res.data.eventLocation,
      isLive: res.data?.isLive || false,
      eventOrganizerId: res.data.organizerId,
      eventBannerUrl: res.data.eventBannerUrl,
      eventCategory: res.data.Category,
      ticketFeeMode: res.data.ticketFeeMode
     // eventUrlName: res.data.eventUrlName
    }
    //console.log("event date for basic info", eventBasicInfo.eventDate);
    dispatch(updateEvent({event: eventBasicInfo}));
    console.log('event in redux',eventBasicInfo);
    return res.data;
  },
  {
    
    staleTime: 1000 * 60 * 5,
    enabled: !!id && isActive
  }
  );



  const {
    control,
    setValue,
    handleSubmit,
    register,
    reset,
    getValues,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(eventSchema),
    defaultValues: {
      tagList: eventDetails?.tags || [],
      fullAddress: eventDetails?.eventLocation || "",
      eventCategory: eventDetails?.category || "",
      lat: eventDetails?.latitude || 0,
      lng: eventDetails?.longitude || 0,
      street: eventDetails?.streetAddress || "",
      zip: eventDetails?.zipCode || "",
      city: eventDetails?.city || "",
      state: eventDetails?.state || "",
      eventName:  eventDetails?.eventName || "",
      eventUrlName: getFullUrlForEvent(eventDetails?.eventUrlName,organizerEventBaseUrl),
      description:  eventDetails?.description || "",
      eventDuration: eventDetails?.duration || 0,
      agenda: eventDetails?.agenda || null,
      headline: eventDetails?.eventHeadline || null,
      eventStartDate: eventDetails ? new Date(eventDetails.eventDate).toLocaleString('sv-SE').slice(0, 16) : "", // format for datetime-local input in local timezone
    },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const updateRedux = (data:any, eventId?:number)=>{

      const eventBasicInfo = {
        eventId: data.eventId || eventId,
        eventName: data.eventName,
        eventHeadline: data.headline,
        eventDate: new Date(data.eventStartDate),
        duration: data.duration,
        eventLocation: data.fullAddress,
        eventOrganizerId: data.organizerId,
        eventBannerUrl: data.eventBannerUrl,
        //ticketFeeMode:data.ticketFeeMode
      }
      dispatch(updateEvent({event: eventBasicInfo}));
      console.log("redux update with even data", eventBasicInfo);
  }
  const eventCache = useAppSelector((state: RootState) => state.event);

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors);
 
    const eventApi ={
      eventId: eventCache.eventId || 0,
      eventName: data.eventName,
      eventUrlName: createUrlSlug(data.eventName),
      //need these 2 fields below only so that the cache on the server has it when we update it.
      organizerUrlName : user?.customerUrlName,
      eventBannerUrl: eventDetails?.eventBannerUrl,
      eventHeadline: data.headline,
      eventDate:new Date(data.eventStartDate).toISOString(),
      duration: data.eventDuration,
      eventLocation: data.fullAddress,
      category: data.eventCategory,
      //use the organizer if from redux (for existing event) or user's id for new event
      eventOrganizerId: eventCache?.eventOrganizerId || user?.customerId,
      isLive: eventCache?.isLive || false,
      ticketFeeMode: eventCache.ticketFeeMode || 0,
      eventDescription: data.description,
      tags: data.tagList?.join(','),
      eventAgenda: data.agenda,
      streetAddress: data.street,
      city:data.city,
      state: data.state,
      zipCode: data.zip,
      latitude: data.lat,
      longitude: data.lng,
     }
    
    console.log('event api data',eventApi);

    if (eventCache.eventId) {
      console.log("event already exists. Updating event for id:",eventCache.eventId);
      axiosClient.put(`/events/${eventCache.eventId}`,eventApi)
      .then(async response => {
        console.log('Event updated response:', response.data);
        if (response.status === 200)
        {
          console.log('Event updated succefully for eventid:',eventCache.eventId);
          toast.success("Event saved successfully");
          await uploadImage(eventApi.eventOrganizerId,eventCache.eventId);
          updateRedux(eventApi);
          queryClient.resetQueries({queryKey:[`events/details/${id}`]});
        }
        else
        {
          console.log('Event update failed for eventid:',eventCache.eventId);
          toast.error("Event save failed.");
        }
      })
      .catch(error => {
        console.error('Error updating event exception:', error);
        toast.error("Event save failed.");
      });
    }
    else
    {
      console.log("Creating new event");
      axiosClient.post(`/events/${eventApi.eventOrganizerId}`,eventApi)
      .then(async response => {
        console.log('Event created response:', response.data);
        if (response.data > 0)
        {
          console.log('Event created succefully with eventid:',response.data);
          toast.success("Event created successfully");
          await uploadImage(eventApi.eventOrganizerId,response.data);
          updateRedux(eventApi,response.data);
          //queryClient.resetQueries({queryKey:[`events/details/${response.data}`]});
          
          setTimeout(() => history.push(`/EventManager/${response.data}/ticketlist`), 1500);
        }
        else
        {
          console.log('Event create failed');
          toast.error("Event create failed.");
        }
      })
      .catch(error => {
        console.error('Error creating event:', error);
        toast.error("Event create failed.");
      });
    }  
      //setTimeout(() =>  window.location.assign(`/EventManager/${eventId}/ticketlist`), 1500);
  }

  const uploadImage = async (organizerId:any,eventId:any)=>{
    if (file)
    {
      const response = await axiosClient.post(`/FileUpload/presigned-url/${organizerId}` ,{
        fileName: file.name,
        eventId: eventId,
        purpose: "EventBannerImage",
        contentType: file.type
      });
      console.log('Response from presigned url is:',response.data);
      if (response.status !== 200)
      {
        console.error('Error updating image:', response?.statusText);
        toast.error("Error updating image");    
        return;
      }
      
      const res =await axios.put(response.data.url, file, {
      headers: {
        "Content-Type": file.type,           // MUST match what was signed
      },
      });   
      if (res.status !== 200)
      {
        console.log('Error uploading image to presigned url', res.data);
        toast.error("Error updating image");  
        return;
      }   
      else
      {
          console.log('Image uploaded successfully, now updating url in database');
          const res  = await axiosClient.put(`/FileUpload/UpdateUrl/${organizerId}`, {
            fileName: file.name,
            eventId: eventId,     
            purpose: "EventBannerImage"
          });
          if (res.status === 200)
          {
            console.log('Image URL updated successfully in database');
            toast.success("Image updated successfully");
          }
          else
          {
            console.error('Error updating image URL in database:', res?.statusText);
            toast.error("Error updating image URL in database");
          }

      }
      console.log('response for image upload', res.data);
    }
  }
  
  useEffect(() => { 
    if (eventDetails) { 
      let tagItems: string[] = [];
      if (eventDetails.tags && eventDetails.tags.length > 0) {
        tagItems = eventDetails.tags.split(',').map((tag: string) => tag.trim());
      }
      if (eventDetails.eventBannerUrl) {
        setImagePreview(eventDetails.eventBannerUrl);
      }
      reset(
      {
        tagList: tagItems,
        fullAddress: eventDetails?.eventLocation || "",
        lat: eventDetails?.latitude || 0,
        lng: eventDetails?.longitude || 0,
        street: eventDetails?.streetAddress || "",
        zip: eventDetails?.zipCode || "",
        city: eventDetails?.city || "",
        state: eventDetails?.state || "",
        eventName:  eventDetails?.eventName || "",
        eventUrlName: getFullUrlForEvent(eventDetails?.eventUrlName,organizerEventBaseUrl),
        description:  eventDetails?.eventDescription || "",
        eventDuration: eventDetails?.duration || 0,
        agenda: eventDetails?.eventAgenda || null,
        headline: eventDetails?.eventHeadline || null,
        eventCategory: eventDetails?.category || null,
        eventStartDate: eventDetails ? new Date(eventDetails.eventDate).toLocaleString('sv-SE').slice(0, 16) : "", // format for datetime-local input in local timezone
      }
    );
    //setValue("description", eventDetails?.description || "");
  }
  }, [eventDetails, reset]);

  if (isLoading) return <p>Loading...</p>;

  return (
    <>
     {/* <Toaster position="top-right" /> */}
{/* //     <form
//       onSubmit={handleSubmit(
//         console.log("address", fullAddress),
//   (data) => console.log("submit fired!", data),
//   (errors) => console.log("validation errors", errors)
// )}> */}
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-2xl mx-auto p-6 space-y-6"
    > 
         
      <div>
        <label className="block font-semibold mb-1">Event Title</label>
        <input
          type="text"
          {...register("eventName")}
          className="w-full border rounded p-2"
          placeholder="Enter event title"
          onChange={(e) => {
                      // Call the RHF onChange first
                      register("eventName").onChange(e); 
                   
                      // Then run your custom logic
                      setValue("eventUrlName", getFullUrlForEvent(createUrlSlug(getValues("eventName")),organizerEventBaseUrl));
                  }}
        />
        {errors.eventName && (
          <p className="text-red-600 text-sm mt-1">{errors.eventName.message}</p>
        )}
      </div>

      <div>
        <label className="block font-semibold mb-1">Event Headline</label>
        <input
          type="text"
          {...register("headline")}
          className="w-full border rounded p-2"
          placeholder="Enter event headline to attract attendees"
        />
        {/* {errors.eventName && (
          <p className="text-red-600 text-sm mt-1">{errors.eventName.message}</p>
        )} */}
      </div>
      
      <FileUpload 
        imagePreview={imagePreview} 
        setImagePreview={setImagePreview} 
        file={file}
        setFile={setFile}
      />
      <div className="flex flex-row justify-items-center justify-between">
        <div className="flex flex-col space-y-1">
          <label className="font-semibold mb-1">Event Date</label>
          <input
            type="datetime-local"
            {...register("eventStartDate")}
            className="border rounded p-2"
            placeholder="Select event date and time"
          />
           <div className="min-h-[20px]">
            {errors.eventStartDate && (
              <p className="text-red-600 text-sm mt-1">
                {errors.eventStartDate.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col space-y-1">
          <label className="ml-auto font-semibold mb-1">Duration (hrs)</label>
          <input 
            type="number"
            {...register("eventDuration")}
            className="ml-auto w-1/4 border rounded p-2"
            placeholder="Duration (hours)"
            min={0}
          />
          <div className="min-h-[20px]">
          {errors.eventDuration && (
            <p className="text-red-600 text-sm mt-1">
              {errors.eventDuration.message}
            </p>
          )}
          </div>
        </div>
      </div>
      
      <div className="space-y-1">
        <label className="font-semibold mb-1">Event Category</label>
        <select
          {...register("eventCategory")}
          className="w-full border rounded p-2"
        >
          <option value="General Event">General Event</option>
          <option value="Museum or Art Gallery">Museum or Art Gallery</option>
          <option value="Conference or Workshop">Conference or Workshop</option>
          <option value="Sporting Event">Sporting Event</option>
          <option value="Concert or Live Performance">Concert or Live Performance</option>
          <option value="Nightclub or Bar Event">Nightclub or Bar Event</option>
        </select>
      </div>
      
       <div className="space-y-1">
        <label className="font-semibold mb-1">Event url</label>
        <input
          type="text"
          {...register("eventUrlName")}
          className="w-full border rounded p-2"
          placeholder="Url for your event"
          title="This is the url for your event."
          disabled
        />
      </div>

      <div>
        <label className="block font-semibold mb-1">Address</label>
        <Controller
          name="fullAddress"
          control={control}
          render={({ field }) => (
            <MapboxAddressField
              value={field.value}
              onSelect={(data: AddressData) => {
                field.onChange(data.fullAddress);
                
                setValue("street", data.street);
                setValue("city", data.city);
                setValue("state", data.state);
                setValue("zip", data.zip);
                setValue("lat", data.lat);
                setValue("lng", data.lng);
               // setValue("fullAddress", data.fullAddress)
                //field.value = data.fullAddress;
                //field.onBlur();
                console.log("Address selected in form:", data);
              }}
            />
          )}
        />
      </div>
     
      
      {/* Rich Text Field 1 */}
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

      {/* Rich Text Field 2 */}
      <div>
        <label className="block font-semibold mb-1">Agenda</label>
        <Controller
          name="agenda"
          control={control}
          render={({ field }) => (
            <RichTextEditor value={field.value ?? ""} onChange={field.onChange} />
          )}
        />
        {errors.agenda && (
          <p className="text-red-600 text-sm mt-1">{errors.agenda.message}</p>
        )}
      </div>
      <div>
        <label className="block font-semibold mb-1">Tags</label>
        <Controller
          name="tagList"
          control={control}
          render={({ field }) => (
            <ListInput 
              items={field?.value || []} 
              onChange={(tags: string[]) => {
                setValue("tagList", tags);    
                console.log("tags selected in form:", tags);
              }}/>           
          )}
        />

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
    </>
  );
}
