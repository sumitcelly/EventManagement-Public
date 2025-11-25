// src/pages/Login.jsx
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
//import { useDispatch, useSelector } from "react-redux";
import { loginUser } from "../features/auth/authSlice";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import axiosClient from "../api/axiosClient";
import React, { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import CountdownTimer from "../components/Countdowntimer";


interface SecureCodeFormInputs {
  secureCode: string;
}

// Yup schema
const schema = yup.object({
  secureCode: yup.string().required("SecureCode is required").length(8,"Secure code must be 8 characters")
});

export default function ValidateSecureCode() {

 // const { status, error } = useAppSelector((state) => state.auth);
  const [apiStatus,setApiStatus] = useState("");
  const [status,setStatus]=useState<"idle"|"loading"|"error">("idle");
  const navigate = useNavigate();
  const location = useLocation();
  const {email} = location.state;
  console.log("validating secure code for email:", email);

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

  const generateemailcode = async (email:string) => {
    console.log("sending secure code to email:", email);
      setApiStatus("");
      setStatus("loading");
      try{
        const res = await axiosClient.get(`/user/generateemailcode/${email}`);
        if (res?.data?.success) {
            setApiStatus("Secure code sent to your email.");
            
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
  }

  const onSubmit = async (data :SecureCodeFormInputs) => {
      console.log("sending secure code to email:", data.secureCode);
      setApiStatus("");
      setStatus("loading");
      try{
        const res = await axiosClient.post(`/user/VerifyEmailCode`,{email:email, password:data.secureCode});
        if (res?.data?.success) {
            setApiStatus("You are logged in.");
            //hookup login logic here
            //navigate("/validatesecurecode",{state:{email:data.email}});      
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
        setApiStatus("Failed to validate secure code. Please try again later.");
        setStatus("error");
    }
    finally {
    setStatus("idle");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
      <h1 className="text-2xl font-bold mb-4">SecureCode Login- Step 2</h1>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* Username */}
        <div>
          <label className="block text-sm font-medium">Enter the SecureCode sent to {email}</label>
          <input
            type="text"
            {...register("secureCode")}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
          {errors.secureCode && (
            <p className="text-red-500 text-sm">{errors.secureCode.message}</p>
          )}
        </div>
        
        <div className="flex flex-row items-center justify-between">
        <CountdownTimer displayString="Generate new code in" timerExpiredCallback={()=>{
            console.log("timer expired - enabling generate new code button");
            <button
                type="button"
                onClick={() => generateemailcode(email)}
                className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400">
                {status === "loading" ? "Logging in..." : "Generate New Code"}
            </button>
        }}/>

        
         <button
            type="submit"
            aria-busy={status === "loading"} 
            disabled={status === "loading"}
            className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
            >
          {status === "loading" ? "Logging in..." : "Login"}
         </button>
        </div>
        {apiStatus && <p className="mt-4 text-green-500">{apiStatus}</p>}

        <a className="mr-auto text-accent-color hover:underline mb-3" 
            onClick={(e=>{
            e.preventDefault();
            navigate("/sendsecurecode",{state:{email:email}});  
            })}>
            Back
        </a>
      </form>
    </div>
  );
}
