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
import FileUpload from "../../components/FileUpload";
import axios from "axios";
import { InfoModal } from "../../components/InfoModal";
import { changeUserRole, getAccessToken } from "../../features/auth/authSlice";
import { createUrlSlug } from "../../utils/StringUtils";

const memberSchema = yup.object({
  orgName: yup.string().required("Organization name required."),
  eventBaseUrl: yup.string().required("Organizer Base Url is required.").default(window.location.origin),
  description: yup.string().required("Organizer company description is required"),
  country: yup.string().required("Country is required").default("US"),
  aboutMe: yup.string().required("Organizer about me is required."),
  imagePreview: yup.string().nullable().default(null),
  organizerPhone: yup
    .string()
    .required("Phone is required.")
    .trim()
    .matches(/^\+?1?[-.\s]?(\(?\d{3}\)?[-.\s]?){2}\d{4}$/, "Enter a valid US phone number."),
  organizerEmail: yup.string().email("Email is invalid").required("Email is required."),
  });

type FormValues = {
  orgName: string;
  eventBaseUrl: string;
  description:string;
  country: string;
  aboutMe:string;
  imagePreview: string | null;
  organizerEmail: string;
  organizerPhone: string;
};

export default function OrganizerAbout({organizerId,organizerInfo}: {organizerId?:string,organizerInfo?:OrganizerInfo}) {

  const [openModal, setOpenModal] = useState(false);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [file, setFile] = useState<File | null>(null);
 
  const user = useAppSelector((state: RootState) => state.auth.user);
  
  const queryClient = useQueryClient();
  const dispatch = useAppDispatch();
  
  const {
    control,
    handleSubmit,
    reset,
    register,
    setValue,
    getValues,
    setError,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(memberSchema),
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const checkUniqueOrgName = async()=>{
    console.log('checking uniqueness of org name...');
    try
    {
      const result = await axiosClient.get(`/EventOrganizer/CheckUniqueOrgName/${createUrlSlug(getValues("orgName"))}`);
      return result.data;
    }
    catch(error)
    {
      console.log('error checking org name', error);
      toast.error('error checking org name availability');
    }
  }

  const onSubmit = async (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    // if (!organizerInfo)
    //   return;

    console.log('image data', imagePreview);
    const apiData={
        organizerId:organizerInfo?.organizerId || 0,
        organizationName: data.orgName,
        organizerEmail: data.organizerEmail,
        organizerWebsite: organizerInfo?.organizerWebsite,
        organizerEventBaseUrl:  createUrlSlug(data.orgName) ,
        organizerDescription: data.description,
        organizerAboutMe: data.aboutMe,
        organizerInstagram: organizerInfo?.organizerInstagram ,
        organizerFacebook: organizerInfo?.organizerFacebook,
        organizerX: organizerInfo?.organizerX,
        organizerPhone: data.organizerPhone,
        organizerCountry: organizerInfo?.organizerCountry,
        stripeAccountId : organizerInfo?.stripeAccountId,
        stripeConnectStatus: organizerInfo?.stripeConnectStatus
    };
    console.log('api data sent to server', apiData);

    if (organizerInfo?.organizerId)
    {    
      try
      {
        const response = await axiosClient.put(`/eventorganizer/${organizerInfo.organizerId}`,apiData)
        if (response.status === 200)
        {
          console.log('Organizer updated successfully:', response.data);
      
          await uploadImage();
          queryClient.invalidateQueries(['Organizer',organizerId]);
          toast.success("Organizer info saved");
        }
        else
        {
           toast.error(`Error saving organizer info ${response.status}`);
        }
      }
      catch(error)
      {
        console.error('Error creating/updating organizer:', error);
        toast.error("Error saving organizer info");     
      };
    }
    else
    {
      try
      {
        const response = await axiosClient.post('/eventorganizer',apiData);
        if (response.status == 200)
        {
            console.log('Organizer created successfully:', response.data);
           
            if (!response.data)
            {
              toast.error('Invalid return after creating organizer');
              return;
            }

            await uploadImage(response.data.accessToken,response.data.user.customerId);
            queryClient.invalidateQueries(['Organizer',response.data.user.customerId]);
            toast.success("Congrats! You have successfully signed up as an organizer.");
            setOpenModal(true);
            dispatch(changeUserRole(response.data));        
            
        }
        else
        {
          toast.error(`Error saving organizer info ${response.status}`);
        }
      }
      catch(error){
        console.error('Error creating/updating organizer:', error);
        toast.error("Error saving organizer info");
        // Handle error (e.g., show notification to user)
      }
    }
  }
  
  const uploadImage = async (token?: string,customerId?:string)=>{
    if (file)
    {
      const  baseApiUrl =import.meta.env.VITE_API_BASE_URL;
      console.log('access token and customer uid',token, customerId);
      const authToken = token ?? getAccessToken();
      const orgId = customerId ?? organizerId;
      const response = await axios.post(`${baseApiUrl}/FileUpload/presigned-url/${orgId}`,{
        fileName: file.name,
        eventId: 0,
        purpose: "OrganizerAboutMeImage",
        contentType: file.type
      }, 
      { 
        headers: {
        'Authorization': `Bearer ${authToken}`,
         'Accept': 'application/json'
      }});
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
          const res  = await axios.put(`${baseApiUrl}/FileUpload/UpdateUrl/${orgId}`, {
            fileName: file.name,
            eventId: 0,
            purpose: "OrganizerAboutMeImage"
          }
          , 
          { 
            headers: {
            'Authorization': `Bearer ${authToken}`,
            'Accept': 'application/json'
          }});
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
    console.log('MemberInfo changed:', organizerInfo);
    if (organizerInfo) {
      const values = {
        organizerId: organizerInfo.organizerId,
        orgName: organizerInfo.organizationName || '',
        organizerEmail: organizerInfo.organizerEmail || '',

        eventBaseUrl: organizerInfo.organizerEventBaseUrl ? window.location.origin+'/'+ organizerInfo.organizerEventBaseUrl:
                       window.location.origin+'/'+createUrlSlug(organizerInfo.organizationName),
        description: organizerInfo.organizerDescription || '',
        //imagePreview: organizerInfo.organizerImageUrl || '',
        aboutMe: organizerInfo.organizerAboutMe || '',
        country: organizerInfo.organizerCountry || 'US',
        organizerPhone: organizerInfo.organizerPhone || '',
        
      };
      console.log('Resetting form with:', values); 
      reset(values);
      setImagePreview(organizerInfo.organizerImageUrl);
    }
   
     
  }, [organizerInfo]);

  if (!user || !user.email)
  {
    return(<div className="text-accent-dark text-lg font-body justify-center max-w-xl mx-auto mt-4">You are not authorized!</div>);
  }

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
       
      <div className="space-y-1">
        <label className="font-semibold mb-1">Organization Name</label>
        <input
          type="text"
          {...register("orgName")}
          className="w-full border rounded p-2"
          placeholder="Enter organization name..."
          onBlur={async (e)=> {
              register("orgName").onBlur(e); 
              //console.log('called on onBlur',getValues("orgName"));
              const result = await checkUniqueOrgName();
              console.log('result from unique check', result);
              if (result === false)
              {
            
                   setError("orgName", {
                    type: "manual",
                    message: "This organization name is already taken.",
                  });
              }
          }}
          onChange={(e) => {
            // Call the RHF onChange first
            register("orgName").onChange(e); 
         
            // Then run your custom logic
            setValue("eventBaseUrl",window.location.origin+'/'+createUrlSlug(getValues("orgName")))
        }}
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
        <label className="font-semibold mb-1">Organizer url</label>
        <input
          type="text"
          {...register("eventBaseUrl")}
          className="w-full border rounded p-2"
          placeholder="The base url for all your events..."
          title="This is the base url that will be used for all your events."
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
          file={file}
          setFile={setFile}
        />
      </div>

      <div className="space-y-1 mt-4">
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
     
      <div className="ml-auto">
        <button
          type="submit"
          className="bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </div>
    {openModal && (
                <InfoModal 
                    modalText="Congrats! You have signed up as an owner. Please complete the information in the other sections. 
                              Connect your account to Stripe if you want to host paid events." 
                    openModal={openModal}
                    onClose={() =>  setOpenModal(false)}
                />
        )}
            
    </form>
  );
}
