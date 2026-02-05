// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { Link, useHistory } from "react-router-dom";
import { useLocation, useParams } from "react-router";
import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
import { TeamMember } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";


const memberSchema = yup.object({
  name: yup.string().required("Name is required."),
  description: yup.string().nullable().default(null),
  subject: yup.string().required("Subject is required."),
  body:  yup.string().required("Body is required."),
  eventName:  yup.string().required("Event name is required.").default(""),
  sendAt: yup.string().default(new Date().toISOString().split('T')[0]),
  sendNow :yup.bool().default(true)
  });

type FormValues = {
  name: string;
  description:string | null;
  subject:string;
  body:string;
  eventName:string;
  sendAt: string;
  sendNow: boolean;
};

export default function CampaignAdd(){

  const [contentChange] = useState(false);

  const location = useLocation();
  const history = useHistory();
  const dispatch = useAppDispatch();
  const user = useAppSelector((state: RootState) => state.auth.user);
  const queryClient = useQueryClient();
  const campaignId = useState<any>(location.state || {});
  const customerId = user?.customerId;

  const { data:events, isLoading:isEventsLoading } = 
  useQuery(['EventsByCustomerId',customerId], async () => {
     console.log("Fetching events for customer", customerId);
     try
     {
      const res = await axiosClient.get(`/Events/ByCustomer/${customerId}`);
      if (res?.data && res.status===200)
      {
          console.log('events fetched from backend',res.data);
          return res.data;
      }
      else
      {
          console.log('events fetched from backend',res.data);
          return [];
      }
    }
    catch(error)
    {
        console.error("Error fetching events:", error);
        toast.error("Error loading report data");
        return [];
    }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!customerId //  only run query if we have an id
    }
  );

  const { data:campaign, isLoading:isCampaignLoading } = 
  useQuery(['CampaignByCampaignId',campaignId], async () => {
     console.log("Fetching campaign for id", campaignId);
     try
     {
      const res = await axiosClient.get(`/byCampaignId/${customerId}`);
      if (res?.data && res.status===200)
      {
          console.log('campign fetched from backend',res.data);
          return res.data;
      }
      else
      {
          console.log('Invalid campaign  id. no data found',res.data);
          toast.error('No data found for campaign');
          return {};
      }
    }
    catch(error)
    {
        console.error(`Error fetching campaing for id: ${campaignId}`, error);
        toast.error("Error loading campaign data");
        return [];
    }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!customerId //  only run query if we have an id
    }
  );

  const {
    control,
    handleSubmit,
    reset,
    register,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(memberSchema),
    defaultValues: {
      name:campaign?.name || "",
      description: campaign?.description || "",
      subject: campaign?.subject || "",
      sendAt: campaign?.sendAt,
      body: campaign?.templateContent,
      eventName:campaign?.eventName
      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });


  const onSubmit = async (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    const postData={
      eventId:data?.eventName,
      emailCampaignName: data?.name,
      description: data?.description,
      templateId:campaign?.templateId,
      templateContent:data?.body,
      sendAt: data?.sendAt,
      sendNow: data?.sendNow,
      subject: data?.subject,
      templateContentChange: true
    };

    console.log('post data for campaign is',postData);
    try
    {
      const result = await axiosClient.post(`/EmailCampaign/addupdatecampaign/${campaignId}`,postData);
      if (result && result.status==200)
      {
        console.log('Campaign updated successfully:', result.data);
        toast.success("Member updated");
        queryClient.invalidateQueries(['CampaignByOrganizer', customerId]);
      }
    }
    catch(error)
    {
        console.error('Error creating/updating camppaign:', error);
        toast.error("Error with email campign.");
    }
  }

   useEffect(() => {
    console.log('events or campaign changes', events,campaign);
    
      const values = {
        name: campaign?.name || '',
        description: campaign?.email || '',
        subject: campaign?.subject || '',
        body: campaign?.body || '',
        sendAt: campaign?.sendAt || '',
        eventName:campaign?.eventName || ''
  
      };
      console.log('Resetting form with:', values);
      reset(values);
 
  }, [events, campaign,reset]);

  return (
    <IonPage>
      <IonHeader>
              <AppNavbar />
      </IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
      <form onSubmit={handleSubmit(onSubmit)}
        className="max-w-md mx-auto mt-4 p-3"
      >  
      <Toaster position="top-right" />
      <div className="flex flex-col">
         <div
              onClick={(e) => {
                e.stopPropagation();
                history.push(`/campaignlist`);
              }}
              className="ml-4 px-3 py-1 text-sm font-body text-white bg-brand-light rounded inline-flex cursor-pointer"
            >
              Back To Campaigns
          </div>
        
        <div className="space-y-1">
          <label className="font-semibold mb-1">Name</label>
          <input
            type="text"
            {...register("name")}
            className="w-full border rounded p-2"
            placeholder="Enter name"
          />
          <div className="min-h-[20px]">
            {errors.name && (
              <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
            )}
          </div>
        </div>
          <div className="space-y-1">
          <label className="font-semibold mb-1">Description</label>
          <input
            type="text"
            {...register("description")}
            className="w-full border rounded p-2"
            placeholder="Enter description"
          />
          {/* <div className="min-h-[20px]">
            {errors.name && (
              <p className="text-red-600 text-sm mt-1">{errors.ndesame.message}</p>
            )}
          </div> */}
        </div>
        <div className="space-y-1">
          <label className="font-semibold mb-1">Select events</label>
          <select
            {...register("eventName")}
            className="w-full border rounded p-2">
            <option value="">{"Select an event"}</option>
            {events.map((event:any) => (
              <option key={event.eventId} value={event.eventId}>
                {event.eventName}
              </option>
            ))}
          </select>
          <div className="min-h-[20px]">
            {errors.eventName && (
              <p className="text-red-600 text-sm mt-1">{errors.eventName.message}</p>
            )}
          </div>
        </div>

        <div className="space-y-1">
          <label className="font-semibold mb-1">Subject</label>
          <input
            type="text"
            {...register("subject")}
            className="w-full border rounded p-2"
            placeholder="Enter email subject"
          />
          <div className="min-h-[20px]">
            {errors.name && (
              <p className="text-red-600 text-sm mt-1">{errors.subject?.message}</p>
            )}
          </div>
        </div>

        <div className="space-y-1">
          <label className="font-semibold mb-1">Body</label>
          <Controller
              name="body"
              control={control}
              render={({ field }) => (
                <RichTextEditor value={field.value ?? ""} onChange={field.onChange} />
              )}
            />
            {errors.body && (
              <p className="text-red-600 text-sm mt-1">{errors.body.message}</p>
            )}
        </div>
        <div className="space-y-1">
          <label className="font-semibold mb-1">Schedule</label>
          <input type="checkbox"   
            className="ml-2 mt-1"
            {...register("sendNow")}>
          </input>
          <div className="flex flex-col mt-2">
          <label className="font-semibold mb-1">Event Date</label>
            <input
              type="datetime-local"
              {...register("sendAt")}
              className="border rounded p-2"
              placeholder="Select event date and time"
            />
            {errors.sendAt && (
              <p className="text-red-600 text-sm mt-1">
                {errors.sendAt.message}
              </p>
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
    </IonContent>
  </IonPage>
  );
}
