// EventForm.tsx
import React, { EventHandler, use, useEffect, useState } from "react";
import { useForm, Controller, set } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";

import axiosClient from "../../api/axiosClient";
import { useQuery, useQueryClient } from "react-query";
import { useParams, useHistory } from "react-router-dom";

import { useAppDispatch, useAppSelector } from "../../app/hook";
import { updateEvent } from "../../features/auth/eventSlice";
import { RootState } from "../../app/store";
import { TeamMember,TeamRoles } from "../../types/Teams";
import toast, { Toaster } from 'react-hot-toast';
import Permissions from "../../components/Permissions";

const memberSchema = yup.object({
  name: yup.string().required("Name is required."),
  email: yup.string().email("Invalid email address format.").required("Email is required.")  ,
  role: yup.string().required("Permissions is required")
  });

type FormValues = {
  name: string;
  email:string;
  role:string;
  
};

export default function MemberAdd({memberInfo, organizerId}: {memberInfo?: TeamMember, organizerId?:string}) {

  const history = useHistory();
  const user = useAppSelector((state: RootState) => state?.auth.user);
  const queryClient = useQueryClient();
  const [serverStatus, setServerStatus] = useState("");
  const [isAdding, setIsAdding] = useState(memberInfo?.orgMemberId?false:true);

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
      role: memberInfo?.role || "",
      },   
      mode: "onChange",          // 👈 validates as user types or changes field
      reValidateMode: "onChange"
  });


  const onSubmit = async (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    
    const orgAPi={
      organizerMemberId:memberInfo?.orgMemberId,
      customerId: organizerId,
      userId: memberInfo?.userId,
      role: data?.role,
      email: data?.email,
      fullName: data?.name,
      isActive: memberInfo?.status=="Active"?true:false
    }
    if (isAdding==false)
    {
      console.log('member data being to server', orgAPi);
      try
      {
        const response = await axiosClient.put(`/EventOrganizerMembers/${organizerId}`,orgAPi);
        if (response.status==200)
        {
          console.log('User updated successfully:', response.data);
          toast.success("Member updated");
          setServerStatus("Member updated successfully.");
          queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);
        }
      }
      catch(error)
      {
        console.error('Error creating/updating event:', error);
        toast.error("Error updating member");
        setServerStatus("Error updating member");
      }
    }
    else
    {
      axiosClient.post(`/EventOrganizerMembers/${organizerId}`,orgAPi)
      .then(response => {
      console.log('Member created successfully:', response.data);
      toast.success("Member created");
      setServerStatus("Member created successfully.");
      queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);
      setIsAdding(false);
    
      setTimeout(() => {
        history.push(`/TeamManager`);
      }, 2000);
      })
      .catch(error => {
        console.error('Error creating/updating event:', error);
        toast.error("Error creating member");
        setServerStatus("Error creating member");
      });
    }
  }

   useEffect(() => {
    console.log('MemberInfo changed:', memberInfo);
    
      const values = {
        name: memberInfo?.name || '',
        email: memberInfo?.email || '',
        role: memberInfo?.role || ''
        //permissions: memberInfo?.permissions?.join(",")  || ''
      };
      console.log('Resetting form with:', values);
      setIsAdding(memberInfo?.orgMemberId?false:true);
      setServerStatus("");
      reset(values);
 
  }, [memberInfo]);

  return (
    <>
     {/* <Toaster position="top-right" /> */}
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-md mx-auto mt-4 p-3"
    >  
   
    <div className="flex flex-col">
     
      <a href={`/teammanager/${organizerId}`} className="mr-auto text-accent-color hover:underline mb-3" 
        onClick={(e=>{
          e.preventDefault();
          history.push(`/teammanager`);
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
          disabled={memberInfo ==null}
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
          disabled={memberInfo ==null}
        />
        <div className="min-h-[20px]">
          {errors.email && (
            <p className="text-red-600 text-sm mt-1">{errors.email.message}</p>
          )}
        </div>
      </div>
     
      <div className="flex flex-col">
        <label className="font-semibold mb-1">Role</label>
       <select
            {...register("role")}
            className="w-full border rounded p-2">
            <option value="">{"Select a role"}</option>
            {Object.values(TeamRoles).map((role:any) => (
              <option key={role} value={role}>
                {role}
              </option>
            ))}
        </select>
        <div className="min-h-[20px]">
          {errors.role && (
            <p className="text-red-600 text-sm mt-1">
              {errors.role.message}
            </p>
          )}
        </div>
      
      </div>
      <div className="flex flex-row">
        
        <div className="space-y-2 max-w-[300px]">
          
          <div className="font-semibold  text-accent-dark">FullAdmin: Can access anything on site except the Stripe connection.</div>
          <div className="font-semibold text-accent-dark">RestrictedAdmin: Manages events but has no access to sales or financial data.</div>
          <div className="font-semibold text-accent-dark">ScanningAgent: Can only access the check-in app and has no access to dashboard.</div>
        
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
      {serverStatus && <p className="text-green-600 text-sm mt-2">{serverStatus}</p>}
    </div>
            
    </form>
    </>
  );
}
