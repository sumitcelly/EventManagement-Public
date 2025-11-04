// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import RichTextEditor from "../../components/RichTextEditor";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { useParams, useNavigate } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
import { TeamMember } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import Permissions from "../../components/Permissions";

const memberSchema = yup.object({
  name: yup.string().required("Name is required."),
  email: yup.string().email("Invalid email address format.").required("Email is required.")  ,
  permissions: yup.string().required("Permissions is required")
  });

type FormValues = {
  name: string;
  email:string;
  permissions:string;
  
};

export default function MemberAdd({memberInfo, organizerId}: {memberInfo?: TeamMember, organizerId?:string}) {

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
    defaultValues: {
      name:memberInfo?.name || "",
      email: memberInfo?.email || "",
      permissions: memberInfo?.permissions?.join(",") || "",
      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });


  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    // if (!memberInfo)
    //   return;
    const orgAPi={
      organizerMemberId:memberInfo?.orgMemberId,
      customerId: organizerId,
      userId: memberInfo?.userId,
      role: data?.permissions,
      email: data?.email,
      fullName: data?.name,
      isActive: memberInfo?.status=="Active"?true:false
    }
    if (memberInfo?.orgMemberId)
    {
      console.log('member data sent to server', orgAPi);
      axiosClient.put(`/EventOrganizerMembers`,orgAPi)
      .then(response => {
      console.log('User updated successfully:', response.data);
      toast.success("Member updated");
      queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);
     
      })
      .catch(error => {
        console.error('Error creating/updating event:', error);
         toast.error("Error updating member");
        // Handle error (e.g., show notification to user)
      });
    }
    else
    {
      axiosClient.post('/EventOrganizerMembers',orgAPi)
      .then(response => {
      console.log('Member created successfully:', response.data);
      toast.success("Member created");
      queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);
      // reset();
      // setTimeout(() => {
      //   navigate(`/TeamManager/${organizerId}`)
      // }, 1000);
      })
      .catch(error => {
        console.error('Error creating/updating event:', error);
        toast.error("Error creating member");
        // Handle error (e.g., show notification to user)
      });
    }
  }

   useEffect(() => {
    console.log('MemberInfo changed:', memberInfo);
    
      const values = {
        name: memberInfo?.name || '',
        email: memberInfo?.email || '',
        permissions: memberInfo?.permissions?.join(",")  || ''
      };
      console.log('Resetting form with:', values);
      reset(values);
 
  }, [memberInfo]);

  return (
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-md mx-auto mt-4 p-3"
    >  
    <Toaster position="top-right" />
    <div className="flex flex-col">
     
      <a href={`/teammanager/${organizerId}`} className="mr-auto text-accent-color hover:underline mb-3" 
        onClick={(e=>{
          e.preventDefault();
          navigate(`/teammanager/${organizerId}`);
        })}>
          Back to member list
      </a>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-full border rounded p-2"
          placeholder="Enter name"
          disabled={memberInfo !=null}
        />
        <div className="min-h-[20px]">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
          )}
        </div>
      </div>
      <div className="space-y-1">
        <label className="font-semibold mb-1">Email</label>
        <input
          type="text"
          {...register("email")}
          className="w-full border rounded p-2"
          placeholder="Enter email..."
          disabled={memberInfo !=null}
        />
        <div className="min-h-[20px]">
          {errors.email && (
            <p className="text-red-600 text-sm mt-1">{errors.email.message}</p>
          )}
        </div>
      </div>
     
      <div className="flex flex-col">
        <label className="font-semibold mb-1">Permissions</label>
        <Controller
          name="permissions"
          control={control}
          render={({ field }) => (
            <Permissions permissionsList={field.value} onChange={field.onChange} />
          )}
        />
        <div className="min-h-[20px]">
          {errors.permissions && (
            <p className="text-red-600 text-sm mt-1">
              {errors.permissions.message}
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
  );
}
