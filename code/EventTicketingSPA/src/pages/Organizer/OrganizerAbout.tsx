// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";

import axiosClient from "../../api/axiosClient";
import { useQuery } from "react-query";
import { useParams, useNavigate } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";

import { RootState } from "../../app/store";
import { TeamMember } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import Permissions from "../../components/Permissions";
import { OrganizerInfo } from "../../types/Organizer";
import FileUpload from "../../components/FileUpload";

const memberSchema = yup.object({
  orgName: yup.string().required("Organization name required."),
  organizerName: yup.string().required("Organizer Name is required."),
  description: yup.string().required("Organizer company description is required"),
  country: yup.string().required("Country is required").default("USA"),
  aboutme: yup.string().required("Organizer about me is required."),
  imageUrl: yup.string().nullable().default(null)
  });

type FormValues = {
  orgName: string;
  organizerName: string;
  description:string;
  country: string;
  aboutme:string;
  imageUrl: string | null;
  
};

export default function OrganizerAbout({organizerId,organizerInfo}: {organizerId?:string,organizerInfo?:OrganizerInfo}) {

  const [imagePreview, setImagePreview] = useState<string | null>(null);

  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  
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
        organizerId: organizerInfo.organizerId,
        orgName: organizerInfo.organizerName || '',
        organizerName: organizerInfo.organizerName || '',
        description: organizerInfo.organizerDescription || '',
        imageUrl: organizerInfo.organizerImageUrl || '',
        aboutMe: organizerInfo.organizerAboutMe || '',
        country: organizerInfo.organizerCountry || ''
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
        <label className="font-semibold mb-1">Organization Name</label>
        <input
          type="text"
          {...register("orgName")}
          className="w-full border rounded p-2"
          placeholder="Enter organization name..."
        />
        <div className="min-h-[20px]">
          {errors.orgName && (
            <p className="text-red-600 text-sm mt-1">{errors.orgName.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Description</label>
        <textarea      
          rows={5}
          {...register("description")}
          className="w-full border rounded p-2"
          placeholder="Talk about your organization a bit..."
        />
        <div className="min-h-[20px]">
          {errors.description && (
            <p className="text-red-600 text-sm mt-1">{errors.description.message}</p>
          )}
        </div>
      </div>
     <div className="space-y-1">
        <label className="font-semibold mb-1">Organizer Name</label>
        <input
          type="text"
          {...register("organizerName")}
          className="w-full border rounded p-2"
          placeholder="Enter your name..."
        />
        <div className="min-h-[20px]">
          {errors.organizerName && (
            <p className="text-red-600 text-sm mt-1">{errors.organizerName.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">About Me</label>
        <textarea      
          rows={2}
          {...register("aboutme")}
          className="w-full border rounded p-2"
          placeholder="A little blurb about you that will show on all even pages..."
        />
        <div className="min-h-[20px]">
          {errors.aboutme && (
            <p className="text-red-600 text-sm mt-1">{errors.aboutme.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1" title="This will show up on all event pages">Upload an image</label>
        <FileUpload 
          imagePreview={imagePreview} 
          setImagePreview={setImagePreview} 
        />
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
