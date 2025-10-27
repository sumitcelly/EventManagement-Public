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
import { TeamMember } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import Permissions from "../../components/Permissions";
import { OrganizerInfo } from "../../types/Organizer";

const memberSchema = yup.object({
  organizerEmail: yup.string().email("Email is invalid").required("Email is required."),
  organizerWebsite: yup.string().nullable().url("url is invalid").default(null),
  organizerInstagram: yup.string().nullable().default(null),
  organizerFacebook: yup.string().nullable().default(null),
  organizerX: yup.string().nullable().default(null),
  organizerPhone: yup.string().required("Phone")
  });

type FormValues = {
  organizerEmail: string;
  organizerWebsite:string | null;
  organizerInstagram:string | null;
  organizerFacebook:string | null;
  organizerX:string | null;
  organizerPhone:string;
};

export default function OrganizerConnect({organizerInfo, organizerId}: {organizerInfo?: OrganizerInfo, organizerId?:string}) {
  
  const {
    control,
    handleSubmit,
    reset,
    register,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(memberSchema),
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    if (!organizerInfo)
      return;

    if (organizerInfo.organizerId)
    {
      axiosClient.post('/organizer/update',data)
      .then(response => {
      console.log('Organizer updated successfully:', response.data);
      toast.success("Organizer info saved");
      })
      .catch(error => {
        console.error('Error creating/updating organizer:', error);
        toast.error("Error saving organizer info");
      
      });
    }
    else
    {
      axiosClient.post('/organizer/add',data)
      .then(response => {
      console.log('Organizer created successfully:', response.data);
         toast.success("Organizer info saved");
      })
      .catch(error => {
        console.error('Error creating/updating organizer:', error);
         toast.error("Error saving organizer info");
        // Handle error (e.g., show notification to user)
      });
    }
  }

   useEffect(() => {
    console.log('MemberInfo changed:', organizerInfo);
    if (organizerInfo) {
      const values = {
        organizerEmail: organizerInfo.organizerEmail || '',
        organizerWebsite: organizerInfo.organizerWebsite || '',
        organizerFacebook: organizerInfo.organizerFacebook || '',
        organizerInstagram: organizerInfo.organizerInstagram || '',
        organizerX: organizerInfo.organizerX || '',
        organizerPhone: organizerInfo.organizerPhone || '',
      };
      console.log('Resetting form with:', values);
      reset(values);
    }
  }, [organizerInfo]);

  return (
    
    // <form
    // className="max-w-md mx-auto mt-8 p-6"
    //   onSubmit={handleSubmit(
    //     console.log("address", fullAddress),
    //   (data) => console.log("submit fired!", data),
    //   (errors) => console.log("validation errors", errors)
    // )}>
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-md mx-auto mt-4 p-3"
    >  

    <div className="flex flex-col">
     <Toaster position="top-right" />
      
      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Email</label>
        <input
          type="text"
          {...register("organizerEmail")}
          className="w-full border rounded p-2"
          placeholder="Enter your contact email..."
        />
        <div className="min-h-[20px]">
          {errors.organizerEmail && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerEmail.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Phone</label>
        <input
          type="text"
          {...register("organizerPhone")}
          className="w-full border rounded p-2"
          placeholder="Enter your contact phone..."
        />
        <div className="min-h-[20px]">
          {errors.organizerPhone && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerPhone.message}</p>
          )}
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Website</label>
        <input
          type="text"
          {...register("organizerWebsite")}
          className="w-full border rounded p-2"
          placeholder="Enter your web url..."
        />
        <div className="min-h-[20px]">
          {errors.organizerWebsite && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerWebsite.message}</p>
          )}
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Instagram</label>
        <input
          type="text"
          {...register("organizerInstagram")}
          className="w-full border rounded p-2"
          placeholder="Enter your instagram url..."
        />
        <div className="min-h-[20px]">
          {errors.organizerInstagram && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerInstagram.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Facebook</label>
        <input
          type="text"
          {...register("organizerFacebook")}
          className="w-full border rounded p-2"
          placeholder="Enter your facebook url..."
        />
        <div className="min-h-[20px]">
          {errors.organizerFacebook && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerFacebook.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer X</label>
        <input
          type="text"
          {...register("organizerX")}
          className="w-full border rounded p-2"
          placeholder="Enter your X url..."
        />
        <div className="min-h-[20px]">
          {errors.organizerX && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerX.message}</p>
          )}
        </div>
      </div>

     
      <div className="ml-auto">
        <button
          type="submit"
          className="bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </div>
            
    </form>
  );
}
