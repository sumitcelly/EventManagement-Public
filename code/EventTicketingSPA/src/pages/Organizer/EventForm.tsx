// EventForm.tsx
import React, { use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";
import FileUpload from "../../components/FileUpload";
import axiosClient from "../../api/axiosClient";
import { useQuery } from "react-query";
import { useParams, useNavigate } from "react-router-dom";
import MapboxAddressField, { AddressData } from "../../components/MapboxAddressField";
import ListInput from "../../components/ListInput";
import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";

const eventSchema = yup.object({
  eventName: yup.string().required("Event name is required"),
  headline: yup.string().nullable().default(null),
  eventStartDate: yup.string().required("Event date is required")
  .test("past-date", "Event start date cannot be in the past", (value) => {
    
      return  new Date(value) >= new Date();
      
    }),
  fullAddress: yup.string().required("Event address is required"),
  eventDuration: yup.number().required("Event duration is required").
  min(1, "Duration cannot be 0."),
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
  eventStartDate: string;
  eventDuration: number;
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


export default function EventForm() {

  const [imagePreview, setImagePreview] = useState<string | null>(null);


  useEffect(() => {
    if (imagePreview) {
      console.log("Image preview available:", imagePreview)
    }
  }, [imagePreview]) // 

  const {id}  = useParams();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  
  const { data:eventDetails, isLoading } = useQuery(`events/details/${id}`, async () => {
    const res = await axiosClient.get(`/events/details/${id}`);
    console.log('Event details from backend', res?.data);
    return res.data;
  },
  {
    //staleTime: 1000 * 60 * 5,
    enabled: !!id
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
    resolver: yupResolver(eventSchema),
    defaultValues: {
      tagList: eventDetails?.tags || [],
      fullAddress: eventDetails?.eventLocation || "",
      eventName:  eventDetails?.eventName || "",
      description:  eventDetails?.description || "",
      eventDuration: eventDetails?.duration || 0,
      agenda: eventDetails?.agenda || null,
      headline: eventDetails?.eventHeadline || null,
      eventStartDate: eventDetails ? new Date(eventDetails.eventDate).toISOString().slice(0,16) : "", // format for datetime-local input
    },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors);
    const eventId = 1;
    if (eventId) {
      const eventBasicInfo = {
        eventId: eventId,
        eventName: data.eventName,
        eventHeadline: data.headline,
        eventDate: new Date(data.eventStartDate),
        eventDuration: data.eventDuration,
        eventLocation: data.fullAddress,
        eventOrganizerId: user?.id ||1  // TODO: replace with actual organizer id
      }

      dispatch(updateEvent({event: eventBasicInfo}));
      navigate(`/organizer/event/${eventId}/tickets`);
    }
  
      
    // axiosClient.post('/events/createorupdate',data)
      //.then(response => {
      //console.log('Event created/updated successfully:', response.data);
      // const eventId = response.data.eventId;
      // if (eventId) {
      //   navigate(`/organizer/event/${eventId}/tickets`);
      //   const eventBasicInfo = {
      //     eventId
      //     eventName: data.eventName,
      //     eventHeadline: data.headline,
      //     eventDate: new Date(data.eventStartDate),
      //     eventDuration: data.eventDuration,
      //     eventLocation: data.fullAddress,
      //   }

      //  dispatch(updateEvent({data}));
    // })
    // .catch(error => {
    //   console.error('Error creating/updating event:', error);
    //   // Handle error (e.g., show notification to user)
    // });
      // Optionally, navigate to another page or show a success message
    
  };
  
  useEffect(() => { 
    if (eventDetails) { 
      let tagItems: string[] = [];
      if (eventDetails.tags && eventDetails.tags.length > 0) {
        tagItems = eventDetails.tags.split(',').map((tag: string) => tag.trim());
      }
      if (eventDetails.eventImageUrl) {
        setImagePreview(eventDetails.eventImageUrl);
      }
      reset(
      {
        tagList: tagItems,
        fullAddress: eventDetails?.eventLocation || "",
        eventName:  eventDetails?.eventName || "",
        description:  eventDetails?.eventDescription || "",
        eventDuration: eventDetails?.eventDuration || 0,
        agenda: eventDetails?.eventAgenda || null,
        headline: eventDetails?.eventHeadline || null,
        eventStartDate: eventDetails ? new Date(eventDetails.eventDate).toISOString().slice(0,16) : "", // format for datetime-local input
      }
    );
    //setValue("description", eventDetails?.description || "");
  }
  }, [eventDetails, reset]);

  if (isLoading) return <p>Loading...</p>;

  return (
//     <form
//       onSubmit={handleSubmit(
//         //console.log("address", fullAddress),
//   (data) => console.log("submit fired!", data),
//   (errors) => console.log("validation errors", errors)
// )}>
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
      />
      <div className="flex flex-row items-center justify-between">
        <div className="flex flex-col">
          <label className="font-semibold mb-1">Event Date</label>
          <input
            type="datetime-local"
            {...register("eventStartDate")}
            className="border rounded p-2"
            placeholder="Select event date and time"
          />
          {errors.eventStartDate && (
            <p className="text-red-600 text-sm mt-1">
              {errors.eventStartDate.message}
            </p>
          )}
        </div>
        <div className="flex flex-col items-end">
          <label className="font-semibold mb-1">Duration (hrs)</label>
          <input 
            type="number"
            {...register("eventDuration")}
            className="ml-auto w-1/4 border rounded p-2"
            placeholder="Duration (hours)"
            min={0}
          />
          {errors.eventDuration && (
            <p className="text-red-600 text-sm mt-1">
              {errors.eventDuration.message}
            </p>
          )}
        </div>
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
              items={field.value || []} 
              onChange={(tags: string[]) => {
                setValue("tagList", tags);    
                console.log("tags selected in form:", tags);
              }}/>           
          )}
        />

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
