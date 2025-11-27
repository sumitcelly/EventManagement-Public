import React, { useState } from "react";
import { useForm, SubmitHandler } from "react-hook-form";
import PasswordFields, { PasswordStatus } from "../components/PasswordFields";
import { useLocation } from "react-router-dom";
import axios from "axios";

interface FormValues {
  name: string;
  password: string;
}


export default function SignupForm()  {

  const {email} =  useLocation().state || {};

  const { register, handleSubmit } = useForm<FormValues>();
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
    axios.post("http://localhost:5220/user/usersignup", {
      name: data.name,
      email: email,
      password: passwordState.password
    }).then(response => {
      console.log("Signup successful:", response.data);
    }).catch(error => {
      console.error("Signup error:", error);
    });

   
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} style={{ maxWidth: 400 }}>
      <h2>Create Account</h2>
    
      <label className="text-xl font-accent text-accent-color mb-2">Please complete the signup for {email}</label>
      {/* NAME FIELD */}
      <label>Name</label>
      <input
        {...register("name", { required: true })}
        type="text"
        placeholder="Your full name"
      />

      {/* PASSWORD COMPONENT 
      receives state from passwordfields 
      to update state locally*/}
      
      <PasswordFields onChange={(state) => setPasswordState(state)} />

      {/* SUBMIT BUTTON */}
      <button type="submit" disabled={!passwordState.valid || !email}>
        Sign Up
      </button>
    </form>
  );
};


