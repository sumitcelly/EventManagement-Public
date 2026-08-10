// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { useParams } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { TeamMember } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import Permissions from "../../components/Permissions";
import { OrganizerInfo } from "../../types/Organizer";
import { urlValidationSchema } from "../../utils/RichTextSchemaValidation";

const memberSchema = yup.object({
  organizerWebsite:  urlValidationSchema.nullable().default(null),
  organizerInstagram: urlValidationSchema.nullable().default(null),
  organizerFacebook: urlValidationSchema.nullable().default(null),
  organizerX: urlValidationSchema.nullable().default(null),
 
  });

type FormValues = {
  organizerWebsite:string | null;
  organizerInstagram:string | null;
  organizerFacebook:string | null;
  organizerX:string | null;
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

   const queryClient = useQueryClient();
   const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    if (!organizerInfo)
      return;

   

    const apiData={
        organizerId:organizerInfo.organizerId || 0,
        organizerName: organizerInfo.organizerName,
        organizationName: organizerInfo.organizationName,
        organizerWebsite: data.organizerWebsite,
        organizerEventBaseUrl: organizerInfo.organizerEventBaseUrl ,
        organizerDescription: organizerInfo.organizerDescription,
        organizerAboutMe: organizerInfo.organizerAboutMe,
        organizerInstagram: data.organizerInstagram ,
        organizerImageUrl: organizerInfo.organizerImageUrl ,
        organizerFacebook: data.organizerFacebook,
        organizerX: data.organizerX,
        organizerCountry: organizerInfo.organizerCountry,
        organizerEmail: organizerInfo.organizerEmail,
        organizerPhone: organizerInfo.organizerPhone,
    };

    if (organizerInfo.organizerId)
    {
      axiosClient.put(`/eventorganizer/${organizerInfo.organizerId}`,apiData)
      .then(response => {
      console.log('Organizer updated successfully:', response.data);
      toast.success("Organizer info saved");
      queryClient.invalidateQueries(['Organizer',organizerId]);
      })
      .catch(error => {
        console.error('Error creating/updating organizer:', error);
        toast.error("Error saving organizer info");
      
      });
    }
  }

   useEffect(() => {
    console.log('MemberInfo changed:', organizerInfo);
    if (organizerInfo) {
      const values = {
        organizerWebsite: organizerInfo.organizerWebsite || '',
        organizerFacebook: organizerInfo.organizerFacebook || '',
        organizerInstagram: organizerInfo.organizerInstagram || '',
        organizerX: organizerInfo.organizerX || ''
      };
      console.log('Resetting form with:', values);
      reset(values);
    }
  }, [organizerInfo]);

  return (

    <>
     {/* <Toaster position="top-right"  containerStyle={{
    zIndex: 99999, // Ensure it's higher than Flowbite's default tab/modal layers
  }} /> */}
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-md mx-auto mt-4 p-3"
    >  

    <div className="flex flex-col">
    
      
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
    </>
  );
}
