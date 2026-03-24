// src/pages/Login.jsx
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
//import { useDispatch, useSelector } from "react-redux";
import { loginUser, loginUserWithSecureCode } from "../features/auth/authSlice";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import axiosClient from "../api/axiosClient";
import React, { use, useEffect, useState } from "react";
import { useHistory, useLocation,  useParams } from "react-router-dom";
import { set, useForm } from "react-hook-form";
import CountdownTimer from "../components/Countdowntimer";
import { IonContent, IonHeader, IonPage, useIonRouter } from "@ionic/react";
import AppNavbar from "../components/Navbar";


interface SecureCodeFormInputs {
  secureCode: string;
}

// Yup schema
const schema = yup.object({
  secureCode: yup.string().required("SecureCode is required").length(8,"Secure code must be 8 characters")
});

export default function ValidateSecureCode() {

  const dispatch = useAppDispatch();
  const { status, error, isAuthenticated } = useAppSelector((state) => state.auth);

  const [apiStatus,setApiStatus] = useState("");
  const [codeStatus,setcodeStatus]=useState<"idle"|"loading"|"error">("idle");
  const [timerExpired,setTimerExpired]=useState(false);
  const history = useHistory();

  const location = useLocation();
  const  userData:any = location.state || {};
  console.log(' data from user user', userData);
  const {email,userExists} =userData;

  console.log("validating secure code for email:", email,userExists);
  const {returnUrl} = useParams<{returnUrl: string}>();
  console.log("returnUrl:", returnUrl);
  
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
      setcodeStatus("loading");

      try{
        const res = await axiosClient.get(`/user/generateemailcode/${email}`);
        if (res?.status) {
            setApiStatus("Secure code sent to your email.");
            
        } 
        else {
            setApiStatus("Failed to send secure code. Please try again later.");
        }
        setcodeStatus("idle");
        setTimerExpired(false);
    }
    catch (err:any) {
        if (err.response && err.response.status === 404) {
            setApiStatus("Email not found. Please check and try again.");
        }
        else
          setApiStatus("Failed to send secure code. Please try again later.");
          setcodeStatus("error");
    }
    finally {
      setcodeStatus("idle");
    }
  }
  useEffect(() => {
    // Generate the initial secure code when the component mounts
    if (isAuthenticated)
    {
      console.log("login succeeded - navigating to returnUrl:", returnUrl);
      if (returnUrl === "signup")
      {
        //if user exists and they came through signup flow, we take them to reset password page since they want to create new password. If user does not exist we take them to signup page.
        if (userExists)
          history.push("/resetpassword");
        else
          history.push("/" + "Signup");
      }
      if (returnUrl === "resetpassword")
      {
        history.push("/" + (returnUrl || ""));      
      }
      if (returnUrl === "myevents")
      {
        history.push("/myevents");
      }
    }
  }
  , [status]);

  const onSubmit = async (data :SecureCodeFormInputs) => {
      console.log("sending secure code to email:", data.secureCode);
      setApiStatus("");
      dispatch(loginUserWithSecureCode({email:email, password:data.secureCode.toUpperCase(), signup: returnUrl==="signup"?true:false}) );
  };

  const getAuthenticationMessage = () => {
    if (status === "loading") return "Logging in...";
    if (status === "failed") return `Login failed: ${error}`;
    if (isAuthenticated) return "Login successful!";
    return "";
  }

  return (
    <IonPage>
          <IonHeader>
            <AppNavbar />
          </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
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

          {!timerExpired && (
          <CountdownTimer displayString="Generate new code in" timerExpiredCallback={()=>{
              console.log("timer expired - enabling generate new code button");
            setTimerExpired(true);
          }}/>)}

          {timerExpired && (
              <a
                  href="#"
                  onClick={(e) =>{ e.preventDefault(); generateemailcode(email)}   }     
                  className="text-blue-600 hover:underline">
                  {codeStatus === "loading" ? "Generating code..." : "New Code"}
              </a>
          )}

          <button
              type="submit"
              aria-busy={status === "loading"} 
              disabled={status === "loading"}
              className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
              >
            {status === "loading" ? "Verifying..." : "Verify Code"}
          </button>
        </div>
        {apiStatus && <p className="mt-2 text-green-500">{apiStatus}</p>}
        {(!apiStatus && status) && <p className="mt-2 text-green-500">{getAuthenticationMessage()}</p>}

        <a href="#" className="mr-auto text-accent-color hover:underline mb-3" 
            onClick={(e=>{
            e.preventDefault();
            history.push(`/auth/sendsecurecode/${returnUrl}?email=`+email);
            })}>
            Back
        </a>
      </form>
    </div>
    </IonContent>
    </IonPage>
  );
}
