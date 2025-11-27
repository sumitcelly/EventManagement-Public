import React, { use, useState } from "react";
import { useForm, SubmitHandler } from "react-hook-form";
import PasswordFields, { PasswordStatus } from "../components/PasswordFields";
import { useLocation, useNavigate } from "react-router-dom";
import axios from "axios";
import toast, { Toaster } from 'react-hot-toast';
import axiosClient from "../api/axiosClient";

interface FormValues {
  name: string;
  password: string;
}


export default function SignupForm()  {

  //const [error,setError] = useState<string | null>(null);
  const {email} =  useLocation().state || {};

  //console.log("SignupForm for email:", email);

  const { register, handleSubmit, formState:{errors} } = useForm<FormValues>({
    defaultValues: {
      name: email ? email.split("@")[0] : ""
    },
    mode: "onChange",          // 👈 validates as user types or changes field
    reValidateMode: "onChange"
    });

  const [passwordState, setPasswordState] = useState<PasswordStatus>({
    password: "",
    confirm: "",
    valid: false,
    errors: []
  });
  const navigate = useNavigate();
  const onSubmit: SubmitHandler<FormValues> = (data) => {
    if (!passwordState.valid) {
      alert("Fix password errors first.");
      return;
    }
    console.log("✅ Submitted data:", data);

    axiosClient.put(`/user/${email}`,  { 
      name: data.name,
      email: email,
      password: passwordState.password
    },
    { headers: {
        'Content-Type': 'application/json'}
    }).then(response => {
      console.log("Signup successful:", response.data);
      toast.success("Signup successful! You can now log in.");
      navigate("/myevents");     
    }).catch(error => {
      console.error("Signup error:", error);
      toast.error("Signup failed. Please try again.");     
    });

   
  };

  return (
     <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
        <Toaster position="top-right" />
        <h1 className="text-2xl font-bold mb-4 text-center">Create Account</h1>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        
        <div className="text-center">
        <label className="text-l font-accent text-accent-color">Please complete the signup for <span className="text-xl text-tertiary-color">{email}</span></label>
        </div>
        {/* NAME FIELD */}
        
        <div>
          <label className="block text-sm font-medium">Name</label>
          <input
            type="text"
            {...register("name", { required: "Name is required" })}
            className="mt-1 block w-full border rounded px-3 py-2"
            placeholder="Your full name"
          />
         {errors.name &&(<p className="text-red-500 text-sm">{errors.name.message}</p>)}

        </div>

        {/* PASSWORD COMPONENT 
        receives state from passwordfields 
        to update state locally*/}
        
        <PasswordFields onChange={(state) => setPasswordState(state)} />

        <div className="flex flex-row">
        {/* SUBMIT BUTTON */}
            <button type="submit" 
                className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
                disabled={!passwordState.valid || !email}>
                Sign Up
            </button>
        </div>
        </form>
    </div>
  );
};


