// src/features/auth/authSlice.js
import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import axios from "axios";
import axiosClient from "../../api/axiosClient";

interface User {
  id: number;
  email: string;
  role: string;
  customerId: string;
  name: string;
  customerUrlName?:string;
  stripeConnectStatus?:string;
  guest:boolean;
  //only populated if an organizer uses their dashboard.
  stripeAcctId?:string;
}

interface AuthState {
    user: User | null;
  isAuthenticated: boolean | false,
  status: string |null,
  error: string | null
}

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  status: "idle",
  error: null,
};

interface LoginFormInputs {
  email: string;
  password: string;
  signup?: boolean;
}

let accessToken: string | null = null;
let  baseApiUrl =import.meta.env.VITE_API_BASE_URL;
console.log("baseApiUrl", baseApiUrl);

// Async login action
export const loginUser = createAsyncThunk(
  "auth/loginUser",
  async (credentials: LoginFormInputs, { rejectWithValue }) => {
    try {
      
      const res = await axios.post(
        `${baseApiUrl}/user/login`,
        credentials,
        { withCredentials: true } // needed for HttpOnly cookies
      );
      accessToken= res.data.accessToken; // Expecting { username: "john", ... }
      return  res.data.user; // Adjust based on your API response
      
    } catch (err :any) {
      return rejectWithValue(err.response?.data || "Login failed");
    }
  }
);


export const loginUserWithSecureCode = createAsyncThunk(
  "auth/loginUserWithSecureCode",
  async (credentials: LoginFormInputs, { rejectWithValue }) => {
    try {
      const res = await axios.post(
        baseApiUrl+"/user/VerifyEmailCode",
        credentials,
        { withCredentials: true } // needed for HttpOnly cookies
      );
      accessToken= res.data.accessToken; // Expecting { username: "john", ... }
      return  res.data.user; // Adjust based on your API response
      
    } catch (err :any) {
      return rejectWithValue(err.response?.data || "Login failed");
    }
  }
);

export const refreshAccessToken = async () => {
  try {
    const response = await axios.post(
       baseApiUrl+"/user/refresh",
      {},
      { withCredentials: true }
    );
    accessToken = response.data.accessToken;
    return accessToken;
  } catch {
    accessToken = null;
    return null;
  }
};

export const getAccessToken = () => {
  if (accessToken)
    return accessToken;
  else if (sessionStorage.getItem('temp_auth_token'))
  {
    accessToken = sessionStorage.getItem('temp_auth_token');
    sessionStorage.removeItem('temp_auth_token');
    return accessToken;
  }
  console.log('no access token found');
}
  

// Async check user session
export const fetchUser = createAsyncThunk(
  "auth/fetchUser",
  async (_, { rejectWithValue }) => {
    try {
      const res = await axios.get(baseApiUrl+"/api/auth/me", {
        withCredentials: true,
      });
      return res.data;
    } catch (err) {
      return rejectWithValue(null);
    }
  }
);

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    logout(state) {
      state.user = null;
      state.isAuthenticated = false;
      state.status = "idle";
      state.error = null;
      accessToken = null;
      sessionStorage.removeItem('temp_auth_token');
    },
    resetError(state){
      state.error = null;
      state.status = null;
    },
    updateCustomerProfile(state,action){
      console.log('updating customer url name',action.payload);
       if (state.user) {
        state.user.customerUrlName = action.payload.customerUrlName;
        state.user.stripeConnectStatus = action.payload.stripeConnectStatus;
        state.user.stripeAcctId = action.payload.stripeAcctId;
      }
    },   
    changeUserRole(state, action){
      console.log('data in change user role',action.payload);
      accessToken = action.payload.accessToken;
      
      if (action.payload.user) {
        state.user = {
          ...state.user,          // Keep existing user data
          ...action.payload.user  // Merge new user data
        };
      }
    },
     loginAsGuest(state, action){
      console.log('data in login as guest',action.payload);
      accessToken = action.payload.accessToken;
      
      if (action.payload.user) {
        state.user = action.payload.user;
        sessionStorage.setItem('temp_auth_token', action.payload.accessToken)
        if (action.payload.user.id >0)
        {
          state.isAuthenticated = true;
          state.status ="succeeded";
        }
      }
    },
     loginNewMember(state, action){
      console.log('data in login as new member',action.payload);
      accessToken = action.payload.accessToken;
      
      if (action.payload.user) {
        state.user = action.payload.user;
        if (action.payload.user.id >0)
        {
          state.isAuthenticated = true;
          state.status ="succeeded";
        }
      }
    }


  },
  extraReducers: (builder) => {
    builder
      .addCase(loginUser.pending, (state) => {
        state.status = "loading";
      })
      .addCase(loginUser.fulfilled, (state, action :PayloadAction<User>) => {
        state.status = "succeeded";
        state.user = action.payload;
        state.isAuthenticated = true;
      })
      .addCase(loginUser.rejected, (state, action: any) => {
        state.status = "failed";
        state.error = action.payload || "Login failed"  ;
      })
      .addCase(loginUserWithSecureCode.pending, (state) => {
        state.status = "loading";
      })
      .addCase(loginUserWithSecureCode.fulfilled, (state, action :PayloadAction<User>) => {
        state.status = "succeeded";
        state.user = action.payload;
        state.isAuthenticated = true;
      })
      .addCase(loginUserWithSecureCode.rejected, (state, action: any) => {
        state.status = "failed";
        state.error = action.payload || "Login failed"  ;
      })
      .addCase(fetchUser.fulfilled, (state, action) => {
        state.user = action.payload;
        state.isAuthenticated = !!action.payload;
      });
  },
});

export const { logout,changeUserRole,updateCustomerProfile, loginAsGuest,loginNewMember, resetError } = authSlice.actions;
export default authSlice.reducer;

