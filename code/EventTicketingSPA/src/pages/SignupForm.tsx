import React, { use, useState } from "react";
import { useForm, SubmitHandler } from "react-hook-form";
import PasswordFields, { PasswordStatus } from "../components/PasswordFields";
import axios from "axios";
import toast, { Toaster } from 'react-hot-toast';
import axiosClient from "../api/axiosClient";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import { useAppSelector } from "../app/hook";
import Footer  from '../components/Footer';

interface FormValues {
  name: string;
  password: string;
}


export default function SignupForm()  {
  const ionRouter = useIonRouter();
  const user =  useAppSelector((state) => state.auth.user);
  if (!user || !user.id) {
    return (
      <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
          <IonContent className="ion-padding flex flex-col justify-center items-center h-full"> 
          <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
            <h1 className="text-2xl font-bold mb-4 text-center">Error</h1>
            <p className="text-red-500 text-center">No authenticated user found. Please complete secure code validation first.</p>
          </div>
          </IonContent>
      </IonPage>
    );
  }

  const email = user.email;
  console.log("SignupForm for email:", email);

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

  const onSubmit: SubmitHandler<FormValues> = (data) => {
    if (!passwordState.valid) {
      alert("Fix password errors first.");
      return;
    }
    console.log("✅ Submitted data:", data);

    axiosClient.put(`/user/${email}`,  { 
      name: data.name,
      email: email,
      userId: user.id,
      password: passwordState.password
    },
    { headers: {
        'Content-Type': 'application/json'}
    }).then(response => {
      console.log("Signup successful:", response.data);
      toast.success("Signup successful! You can now log in.");
      ionRouter.push("/myevents");
    }).catch(error => {
      console.error("Signup error:", error);
      toast.error("Signup failed. Please try again.");     
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
          <h1 className="text-2xl font-bold mb-4 text-center">Create Account</h1>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          
          <div className="text-center">
          <label className="text-l font-accent text-accent-color">Please complete the signup for <span className="text-xl text-tertiary-color">{email}</span></label>
          </div>
          {/* NAME FIELD */}
          
          <div>
             <input 
                id="hidden-username"
                type="email" // 👈 Using type="email" helps managers index it accurately
                name="username"
                value={email} // 👈 Your logged-in user context email
                autoComplete="username"
                readOnly
                 style={{
                  position: 'absolute',
                  width: '1px',
                  height: '1px',
                  padding: '0',
                  margin: '-1px',
                  overflow: 'hidden',
                  clip: 'rect(0, 0, 0, 0)',
                  border: '0',
                }}
              />
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

          <div className="flex flex-col">
            <label className="flex items-start space-x-2 text-xs text-slate-600 max-w-sm mx-auto">
              <input type="checkbox" required className="mt-0.5 h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
              <span>
                I explicitly accept {import.meta.env.VITE_COMPANY_NAME}'s {' '}
                <a href="/tos" className="text-indigo-600 underline hover:text-indigo-500">Terms of Service</a>{' '}
                and acknowledge the data rules outlined in the{' '}
                <a href="/privacypolicy" className="text-indigo-600 underline hover:text-indigo-500">Privacy Policy</a>.
              </span>
            </label>

          {/* SUBMIT BUTTON */}
              <button type="submit" 
                  className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
                  disabled={!passwordState.valid || !email}>
                  Sign Up
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


