// src/pages/Login.jsx
import { set, useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
//import { useDispatch, useSelector } from "react-redux";
import { loginUser } from "../features/auth/authSlice";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import axiosClient from "../api/axiosClient";
import React, { useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { useLocation} from "react-router-dom";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";

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
  const ionRouter = useIonRouter();
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const email = params.get('email') || '';
  const {returnUrl} = useParams<{returnUrl: string}>();
  const history = useHistory();
 
  console.log("SendSecureCode returnUrl:", returnUrl);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = 
    useForm<SecureCodeFormInputs>(
    { resolver: yupResolver(schema),
        defaultValues: {
            email: email || ""
        },
        mode: "onChange",
        reValidateMode: "onChange"
     },);

  const onSubmit = async (data :SecureCodeFormInputs) => {
      console.log("sending secure code to email:", data.email);
      setApiStatus("");
      setStatus("loading");
      try
      {        
        let url=`/user/GenerateEmailCode/${data.email}`;
        let userExists=false;
        if(returnUrl==="signup")
        {
            url+="/true";
            console.log("Checking if user exists for signup flow");
            try
            {
              const res = await axiosClient.get(`/user/checkuserexists/${data.email}`);
              if(res?.status===200 && res.data===true){
                  userExists=true;
                  console.log("User exists with email:", data.email);
              }
              else if (res.status == 404)
              {
                setStatus("idle");
              }
            }
            catch(err)
            {
              console.log('Error checking user exists',err);
              setStatus("idle");
            }
        }
        const res = await axiosClient.get(url);
        console.log("API response for generate email code:", res);
        
        //if 200 or 404 we show the same message to avoid email enumeration. Only if it's other error we show generic failure message.
        if (res?.status === 200 || res?.status === 404) {
            setApiStatus("If an email exists for your account, you will receive an authentication code to login.");
            history.push(`/auth/validatesecurecode/${returnUrl}`,{userExists: userExists, email: data.email});
          }
        else {
            setApiStatus("Failed to send secure code. Please try again later.");
        }
        setStatus("idle");
    }
    catch (err: any) {
        if (err.response && err.response.status === 404) {
            setApiStatus("Email not found. Please check and try again.");
        }
        else
            setApiStatus("Failed to send secure code. Please try again later.");
        setStatus("error");
        console.error("Error sending secure code:", err);
    }
    finally {
    setStatus("idle");
    }
  };

  return (
    <IonPage>
          <IonHeader>
            <AppNavbar />
          </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
      <h1 className="text-2xl font-bold mb-4">SecureCode Login- Step1</h1>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* Username */}
        <div>
          <label className="block text-sm font-medium">Please enter the email used with our site</label>
          <input
            type="email"
            {...register("email")}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
          {errors.email && (
            <p className="text-red-500 text-sm">{errors.email.message}</p>
          )}
        </div>
        
        <div className="flex items-center justify-between flex-row">
            <button
            type="submit"
            aria-busy={status === "loading"} 
            disabled={status === "loading"}
            className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
            >
            {status === "loading" ? "Sending code..." : "Send Code"}
        </button>

        </div>
        {apiStatus && <p className="mt-4 text-red-500">{apiStatus}</p>}
      </form>
    </div>
    </IonContent>
    </IonPage>
  );
}
