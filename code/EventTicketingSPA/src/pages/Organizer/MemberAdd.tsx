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

const memberSchema = yup.object({
  name: yup.string().required("Name is required."),
  email: yup.string().required("Email is required.").email("Invalid email address format.") ,
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
  
  const {
    control,
    handleSubmit,
    register,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: yupResolver(memberSchema),
    defaultValues: {
      name:memberInfo?.name || "",
      email: memberInfo?.email || "",
      permissions: memberInfo?.permissions.join(",") || "",
      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    //make sure to convert date time local to UTC before sending to backend
    if (!data)
      return;

    if (memberInfo?.userId)
    {
      axiosClient.post('/user/update',data)
      .then(response => {
      console.log('User updated successfully:', response.data);
      })
      .catch(error => {
        console.error('Error creating/updating event:', error);
        // Handle error (e.g., show notification to user)
      });
    }
    else
    {
      axiosClient.post('/user/add',data)
      .then(response => {
      console.log('User created successfully:', response.data);
      })
      .catch(error => {
        console.error('Error creating/updating event:', error);
        // Handle error (e.g., show notification to user)
      });
    }
  }



  return (
    
    // <form
    // className="max-w-2xl mx-auto p-6 space-y-6"
    //   onSubmit={handleSubmit(
    //     //console.log("address", fullAddress),
    //   (data) => console.log("submit fired!", data),
    //   (errors) => console.log("validation errors", errors)
    // )}>
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-2xl mx-auto p-6 space-y-6"
    > 
     <Toaster position="top-right" />
      <a href={`/teammanager/${organizerId}/ticketlist`} className="mr-auto text-accent-color">
          Back to member list
      </a>
     
      <div  className="min-h-[80px]">
        <label className="block font-semibold mb-1">Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-full border rounded p-2"
          placeholder="Enter name"
        />
        <div className="h-5">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
          )}
        </div>
      </div>
      
      <div className="flex flex-col w-1/5 min-h-[80px]">
        <label className="font-semibold mb-1">Email</label>
        <input
          type="text"
          {...register("email")}
          className="border rounded p-2"
          placeholder="Enter email..."
        />
         <div className="h-5">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.email?.message}</p>
          )}
        </div>
      </div>
      
      <div className="flex flex-col w-1/5">
        <label className="block font-semibold mb-1">Permissions</label>
        <Controller
          name="permissions"
          control={control}
          render={({ field }) => (
            <Permissions permissionsList={field.value} onChange={field.onChange} />
          )}
        />
        <div className="h-5">
          {errors.permissions && (
            <p className="text-red-600 text-sm mt-1">
              {errors.permissions.message}
            </p>
          )}
        </div>
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
  );
}
