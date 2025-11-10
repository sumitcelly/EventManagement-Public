// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
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
  eventBaseUrl: yup.string().required("Event Base Url is required."),
  description: yup.string().required("Organizer company description is required"),
  country: yup.string().required("Country is required").default("USA"),
  aboutMe: yup.string().required("Organizer about me is required."),
  imageUrl: yup.string().nullable().default(null)
  });

type FormValues = {
  orgName: string;
  eventBaseUrl: string;
  description:string;
  country: string;
  aboutMe:string;
  imageUrl: string | null;
  
};

export default function OrganizerAbout({organizerId,organizerInfo}: {organizerId?:string,organizerInfo?:OrganizerInfo}) {

  const [imagePreview, setImagePreview] = useState<string | null>(null);

  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  const queryClient = useQueryClient();
  
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


    const apiData={
        organizerId:organizerInfo.organizerId || 0,
        organizationName: data.orgName,
        organizerEmail: organizerInfo.organizerEmail,
        organizerWebsite: organizerInfo.organizerWebsite,
        organizerEventBaseUrl: data.eventBaseUrl ,
        organizerDescription: data.description,
        organizerAboutMe: data.aboutMe,
        organizerInstagram: organizerInfo.organizerInstagram ,
        organizerImageUrl: data.imageUrl ,
        organizerFacebook: organizerInfo.organizerFacebook,
        organizerX: organizerInfo.organizerX,
        organizerPhone: organizerInfo.organizerPhone,
        organizerCountry: organizerInfo.organizerCountry,
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
    else
    {
      axiosClient.post('/eventorganizer',apiData)
      .then(response => {
      console.log('Organizer created successfully:', response.data);
         toast.success("Organizer info saved");
         queryClient.invalidateQueries(['Organizer',organizerId]);
      })
      .catch(error => {
        console.error('Error creating/updating organizer:', error);
         toast.error("Error saving organizer info");
        // Handle error (e.g., show notification to user)
      });
    }
  }
      function createUrlSlug(inputString:String) {
      // Remove special characters (keep alphanumeric and spaces)
      let cleanedString = inputString.replace(/[^a-zA-Z0-9\s]/g, '');

      // Replace spaces with hyphens
      let slug = cleanedString.replace(/\s+/g, '');

      // Convert to lowercase
      slug = slug.toLowerCase();

      return slug;
    }

   useEffect(() => {
    console.log('MemberInfo changed:', organizerInfo);
    if (organizerInfo) {
      const values = {
        organizerId: organizerInfo.organizerId,
        orgName: organizerInfo.organizationName || '',
        eventBaseUrl: organizerInfo.organizerEventBaseUrl || window.location.origin+'/'+createUrlSlug(organizerInfo.organizationName),
        description: organizerInfo.organizerDescription || '',
        imageUrl: organizerInfo.organizerImageUrl || '',
        aboutMe: organizerInfo.organizerAboutMe || '',
        country: organizerInfo.organizerCountry || 'USA'
      };
      console.log('Resetting form with:', values);
      reset(values);
    }
  }, [organizerInfo]);

  return (
    
    // <form
    // className="max-w-md mx-auto mt-8 p-6"
    //   onSubmit={handleSubmit(
        
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
        <label className="font-semibold mb-1">Event base url</label>
        <input
          type="text"
          {...register("eventBaseUrl")}
          className="w-full border rounded p-2"
          placeholder="The base url for all your events..."
          disabled
        />
        <div className="min-h-[20px]">
          {errors.eventBaseUrl && (
            <p className="text-red-600 text-sm mt-1">{errors.eventBaseUrl.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">About Me</label>
        <textarea      
          rows={2}
          {...register("aboutMe")}
          className="w-full border rounded p-2"
          placeholder="A little blurb about your company or self that will show on all event pages..."
        />
        <div className="min-h-[20px]">
          {errors.aboutMe && (
            <p className="text-red-600 text-sm mt-1">{errors.aboutMe.message}</p>
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
