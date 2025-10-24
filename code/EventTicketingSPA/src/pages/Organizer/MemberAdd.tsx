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
    
    if (!memberInfo)
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

   useEffect(() => {
    console.log('MemberInfo changed:', memberInfo);
    if (memberInfo) {
      const values = {
        name: memberInfo.name || '',
        email: memberInfo.email || '',
        permissions: memberInfo.permissions?.join(",") || ''
      };
      console.log('Resetting form with:', values);
      reset(values);
    }
  }, [memberInfo]);

  return (
    
    <form
    className="max-w-md mx-auto mt-8 p-6"
      onSubmit={handleSubmit(
        //console.log("address", fullAddress),
      (data) => console.log("submit fired!", data),
      (errors) => console.log("validation errors", errors)
    )}>
    {/* <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-md mx-auto mt-8 p-6"
    >  */}

    <div className="flex flex-col items-center w-full">
     <Toaster position="top-right" />
      <a href={`/teammanager/${organizerId}`} className="mr-auto text-accent-color hover:underline mb-3">
          Back to member list
      </a>
      <div className="w-full space-y-1">
        <label className="block font-semibold mb-1">Name</label>
        <input
          type="text"
          {...register("name")}
          className="w-1/2 border rounded p-2"
          placeholder="Enter name"
        />
        <div className="min-h-[20px]">
          {errors.name && (
            <p className="text-red-600 text-sm mt-1">{errors.name.message}</p>
          )}
        </div>
      </div>
      <div className="w-full space-y-1">
        <label className="block font-semibold mb-1">Email</label>
        <input
          type="text"
          {...register("email")}
          className="w-1/2 border rounded p-2"
          placeholder="Enter email..."
        />
        <div className="min-h-[20px]">
          {errors.email && (
            <p className="text-red-600 text-sm mt-1">{errors.email.message}</p>
          )}
        </div>
      </div>
      <div className="w-full ">
        <div className="flex flex-col w-1/5">
          <label className="block font-semibold mb-1">Permissions</label>
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
      </div>
      
     
      <div className="flex flex-row items-center justify-between">
        <button
          type="submit"
          className="ml-auto bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </div>
            
    </form>
  );
}
