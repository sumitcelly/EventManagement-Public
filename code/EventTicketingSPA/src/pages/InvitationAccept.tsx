import React, { use, useState } from "react";
import { useForm, SubmitHandler } from "react-hook-form";
import PasswordFields, { PasswordStatus } from "../components/PasswordFields";
import { useLocation, useHistory } from "react-router-dom";
import axios from "axios";
import toast, { Toaster } from 'react-hot-toast';
import axiosClient from "../api/axiosClient";
import { useAppSelector } from "../app/hook";
import AppNavbar from "../components/Navbar";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import Footer from "../components/Footer";
import { useQuery } from "react-query";
import { loginNewMember } from "../features/auth/authSlice";
import { useDispatch } from "react-redux";


interface FormValues {
  password: string;
}


export default function InviatationAccept()  {  
  const location = useLocation();
  const searchParams = new URLSearchParams(location.search);
  const token = searchParams.get('token');
  console.log("Token from query string:", token);
  
  if (!token) {
    return (
      <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
        <h1 className="text-2xl font-bold mb-4 text-center">Error</h1>
        <p className="text-red-500 text-center">No invitation token provided. Please use the link from your email.</p>
      </div>
    );
  }

   const { data:memberData, isLoading } = useQuery(['ValidateToken',token], async () => { 
    try {
      const response = await axiosClient.get(`/eventorganizermembers/validateToken/${token}`);
      console.log("Token validation response:", response.data);
      if (response.status === 200 && response.data)
      {
        return response.data; // Assuming the API returns member details on successful validation
      } 
      else
      {
          throw new Error("Invalid token");
      }
    } catch (error) {
      toast.error("Token validation error:"+ error);
      console.error("Token validation error:", error);
    }
  }, 
  {
    enabled: !!token, // Only run this query if token exists
    staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
    cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
  });

  const { register, handleSubmit, formState:{errors} } = useForm<FormValues>();
  const [passwordState, setPasswordState] = useState<PasswordStatus>({
    password: "",
    confirm: "",
    valid: false,
    errors: []
  });
  const history = useHistory();
  const dispatch = useDispatch();

  const onSubmit: SubmitHandler<FormValues> = async (data) => {

    if (!memberData || !memberData.email) {
      toast.error("Member data is missing. Cannot set password.");
      return;
    }
    if (!passwordState.valid) {
      alert("Fix password errors first.");
      return;
    }
    console.log("✅ Submitted data:", data);

    try
    {
      const result = await axiosClient.post(`/user/setnewmemberpassword`,  { 
        email: memberData.email,
        password: passwordState.password,
        invitationtoken: token,
        username: memberData.fullName
      },
      { headers: {
          'Content-Type': 'application/json'}
      });
      if (result && result.status === 200) {
        console.log("Set password successful:", result.data);
        toast.success("Password set successfully! Logging you in.");
        dispatch(loginNewMember(result.data));    
        history.push("/dashboard");
      }
      else
      {
        console.error("Set password failed:", result);
        toast.error("Set password failed. Please try again." + (result.data || ""));
      }
    }
    catch(error)
    {
      console.error("Set password error", error);
      toast.error("Set password failed. Please try again.");     
    };
   
  };

  if (isLoading) {
    return (
      <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
        <h1 className="text-2xl font-bold mb-4 text-center">Validating Token...</h1>
        <p className="text-gray-500 text-center">Please wait while we validate your invitation token.</p>
      </div>
    );
  }
  else if (!memberData) {
    return (
      <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
        <h1 className="text-2xl font-bold mb-4 text-center">Invalid Token</h1>  
        <p className="text-red-500 text-center">The invitation token is invalid or has expired. Please contact the organizer for a new invitation.</p>
      </div>
    );
  }
  return (
    <IonPage>
    <IonHeader>
      <AppNavbar />
    </IonHeader>
    <IonContent>
      <div className="flex flex-col  min-h-full">
        <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
            <Toaster position="top-right" />
            <h1 className="text-2xl font-bold mb-4 text-center">Set Password</h1>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            
            <div className="text-center">
            <label className="text-l font-accent text-accent-color">Please enter your password for <span className="text-xl text-tertiary-color">{memberData.email}</span></label>
            </div>

            {/* PASSWORD COMPONENT 
            receives state from passwordfields 
            to update state locally*/}
            
            <PasswordFields onChange={(state) => setPasswordState(state)} />

            <div className="flex flex-row">
            {/* SUBMIT BUTTON */}
                <button type="submit" 
                    className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
                    disabled={!passwordState.valid || !memberData.email}>
                    Reset Password
                </button>
            </div>
            </form>
        </div>
        <Footer/>
      </div>
    </IonContent>
  </IonPage>

  );
};


