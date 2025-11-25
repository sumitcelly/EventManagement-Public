// src/pages/Login.jsx
import { set, useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
//import { useDispatch, useSelector } from "react-redux";
import { loginUser } from "../features/auth/authSlice";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import axiosClient from "../api/axiosClient";
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";


interface SecureCodeFormInputs {
  email: string;
}

// Yup schema
const schema = yup.object({
  email: yup.string().required("Email is required").email("Invalid email format")
});

export default function SendSecureCode() {

 // const { status, error } = useAppSelector((state) => state.auth);
  const [apiStatus,setApiStatus] = useState("");
  const  [status,setStatus]=useState<"idle"|"loading"|"error">("idle");
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = 
    useForm<SecureCodeFormInputs>(
    { resolver: yupResolver(schema),
        mode: "onChange",
        reValidateMode: "onChange"
     },);

  const onSubmit = async (data :SecureCodeFormInputs) => {
      console.log("sending secure code to email:", data.email);
      setApiStatus("");
      setStatus("loading");
      try{
        const res = await axiosClient.get(`/user/generateemailcode/${data.email}`);
        if (res?.data?.success) {
            setApiStatus("Secure code sent to your email.");
            navigate("/validatesecurecode",{state:{email:data.email}});      
        } 
        else  if (res?.status === 404 ) {
            setApiStatus("Email not found. Please check and try again.");
        } 
        else {
            setApiStatus("Failed to send secure code. Please try again later.");
        }
        setStatus("idle");
    }
    catch (err) {
        setApiStatus("Failed to send secure code. Please try again later.");
        setStatus("error");
    }
    finally {
    setStatus("idle");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
      <h1 className="text-2xl font-bold mb-4">SecureCode Login- Step1</h1>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* Username */}
        <div>
          <label className="block text-sm font-medium">Email</label>
          <input
            type="email"
            {...register("email")}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
          {errors.email && (
            <p className="text-red-500 text-sm">{errors.email.message}</p>
          )}
        </div>

        <button
          type="submit"
          aria-busy={status === "loading"} 
          disabled={status === "loading"}
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
        >
          {status === "loading" ? "Sending code..." : "Send Code"}
        </button>
        
        {apiStatus && <p className="mt-4 text-green-500">{apiStatus}</p>}
      </form>
    </div>
  );
}
