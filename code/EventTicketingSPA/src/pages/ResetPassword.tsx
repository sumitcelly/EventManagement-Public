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


interface FormValues {
  password: string;
}


export default function ResetPassword()  {

  //const [error,setError] = useState<string | null>(null);


  const user =  useAppSelector((state) => state.auth.user);
  if (!user || !user.email) {
    return (
      <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
        <h1 className="text-2xl font-bold mb-4 text-center">Error</h1>
        <p className="text-red-500 text-center">No authenticated user found. Please complete secure code validation first.</p>
      </div>
    );
  }
  const email = user.email;
  console.log("ResetPassword for email:", email);
  
  //console.log("SignupForm for email:", email);
  const userId  = useAppSelector((state) => state.auth.user?.id);
  const { register, handleSubmit, formState:{errors} } = useForm<FormValues>();
  const [passwordState, setPasswordState] = useState<PasswordStatus>({
    password: "",
    confirm: "",
    valid: false,
    errors: []
  });
  const history = useHistory();
  const onSubmit: SubmitHandler<FormValues> = (data) => {
    if (!passwordState.valid) {
      alert("Fix password errors first.");
      return;
    }
    console.log("✅ Submitted data:", data);

    axiosClient.put(`/user/${email}`,  { 
      email: email,
      password: passwordState.password,
      userId: userId
    },
    { headers: {
        'Content-Type': 'application/json'}
    }).then(response => {
      console.log("Reset password successful:", response.data);
      toast.success("Reset successful! Logging you in.");
      history.push("/myevents");
    }).catch(error => {
      console.error("Signup error:", error);
      toast.error("Reset failed. Please try again.");     
    });
   
  };

  return (
    <IonPage>
    <IonHeader>
      <AppNavbar />
    </IonHeader>
    <IonContent>
      <div className="flex flex-col  min-h-full">
        <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
            <Toaster position="top-right" />
            <h1 className="text-2xl font-bold mb-4 text-center">Reset Password</h1>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            
            <div className="text-center">
            <label className="text-l font-accent text-accent-color">Please enter the new password for <span className="text-xl text-tertiary-color">{email}</span></label>
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


