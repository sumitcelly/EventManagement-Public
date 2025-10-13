// EventForm.tsx
import React, { useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";
import FileUpload from "../../components/FileUpload";
import axiosClient from "../../api/axiosClient";
import { useQuery } from "react-query";
import { useParams, useNavigate } from "react-router-dom";
import MapboxAddressField, { AddressData } from "../../components/MapboxAddressField";

const eventSchema = yup.object({
  eventName: yup.string().required("Event name is required"),
  eventStartDate: yup.string().required("Event date is required"),
  fullAddress: yup.string().required("Event address is required"),
  eventDuration: yup.number().required("Event duration is required"),
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

    const { data:eventDetails, isLoading } = useQuery(`events/details/${id}`, async () => {
      const res = await axiosClient.get(`/events/details/${id}`);
      console.log('Event details from backend', res?.data);
      return res.data;
    },
    {
      staleTime: 1000 * 60 * 5,
      enabled: !!id
    }
    );


  const {
    control,
    setValue,
    handleSubmit,
    register,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(eventSchema),
    defaultValues: {
      eventName:  eventDetails?.eventName || "",
      description:  eventDetails?.description || "",
      eventDuration: eventDetails?.duration || 0,
      agenda: eventDetails?.agenda || null,
      eventStartDate: eventDetails ? new Date(eventDetails.eventDate).toISOString().slice(0,16) : "", // format for datetime-local input
    },
  });

  const onSubmit = (data: FormValues) => {
    console.log("✅ Submitted data:", data);
  };

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
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
      
      <FileUpload 
        imagePreview={imagePreview} 
        setImagePreview={setImagePreview} 
      />
      <div className="flex flex-row items-center justify-between">
        <div>
          <label className="block font-semibold mb-1">Event Date</label>
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
        <div>
          <label className="block font-semibold mb-1">Event duration (hrs)</label>
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

      <button
        type="submit"
        className="ml-auto bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
      >
        Submit
      </button>
    </form>
  );
}
