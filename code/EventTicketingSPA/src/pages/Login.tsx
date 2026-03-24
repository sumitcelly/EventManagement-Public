// src/pages/Login.jsx
import { get, useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
//import { useDispatch, useSelector } from "react-redux";
import { loginUser } from "../features/auth/authSlice";
import { useAppDispatch ,useAppSelector} from "../app/hook";

import { IonPage, IonContent, IonHeader,IonRoute, useIonRouter } from '@ionic/react';
import AppNavbar from "../components/Navbar";
import { useHistory } from "react-router";

interface LoginFormInputs {
  email: string;
  password: string;
}

// Yup schema
const schema = yup.object({
  email: yup.string().required("Email is required"),
  password: yup.string().required("Password is required"),
});

export default function Login() {
  const dispatch = useAppDispatch();
  const { status, error } = useAppSelector((state) => state.auth);

  const history = useHistory();
  
  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<LoginFormInputs>({ resolver: yupResolver(schema) });

  const onSubmit = (data :LoginFormInputs) => {
    dispatch(loginUser(data));
  };

  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
    <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow rounded">
      <h1 className="text-2xl font-bold mb-4">Login</h1>

      {/* {error && <p className="text-red-500 text-sm">{error}</p>} */}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* Username */}
        <div>
          <label className="block text-sm font-medium">Email</label>
          <input
            type="text"
            {...register("email")}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
          {errors.email && (
            <p className="text-red-500 text-sm">{errors.email.message}</p>
          )}
        </div>

        {/* Password */}
        <div>
          <label className="block text-sm font-medium">Password</label>
          <input
            type="password"
            {...register("password")}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
          {errors.password && (
            <p className="text-red-500 text-sm">{errors.password.message}</p>
          )}
        </div>
        <div className="flex flex-row items-center justify-between">
          <a href="#" className="text-sm text-blue-600 hover:underline"
          onClick={(e) => {e.preventDefault(); 
                  history.push(`/auth/sendsecurecode/resetpassword?email=`+getValues('email')); 
                  
                }}
          >Forgot Password?
          </a>
          <button
            type="submit"
            disabled={status === "loading"}
            className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
          >
            {status === "loading" ? "Logging in..." : "Login"}
          </button>
        </div>
      </form>
    </div>
    </IonContent>
    </IonPage>
  );
}
