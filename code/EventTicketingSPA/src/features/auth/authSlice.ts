// src/features/auth/authSlice.js
import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import axios from "axios";
import { RootState } from "../../app/store";

interface User {
  id: number;
  email: string;
  role: string;
  customerId: string;
  name: string;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean | false,
  status: string |null,
  error: string | null,
  token: string | null 
}

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  status: "idle",
  error: null,
  token: null
};

interface LoginFormInputs {
  email: string;
  password: string;
  signup?: boolean;
}

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
      return res.data; // Return both user and accessToken
      
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
      return res.data; // Return both user and accessToken
      
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
    return response.data.accessToken;
  } catch {
    return null;
  }
};

//export const getAccessToken = (state: RootState) => state.auth.token;


// Async check user session
export const fetchUser = createAsyncThunk(
  "auth/fetchUser",
  async (_, { rejectWithValue }) => {
    try {
      const res = await axios.get(baseApiUrl+"/user/me", {
        withCredentials: true,
      });
      console.log("fetch user response", res.data);
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
      state.token = null;
    },
     setToken(state, action: PayloadAction<string | null>) {
      state.token = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loginUser.pending, (state) => {
        state.status = "loading";
      })
      .addCase(loginUser.fulfilled, (state, action :PayloadAction<any>) => {
        state.status = "succeeded";
        state.user = action.payload.user;
        state.isAuthenticated = true;
        state.token = action.payload.accessToken || null; // Store token if returned by API
      })
      .addCase(loginUser.rejected, (state, action: any) => {
        state.status = "failed";
        state.error = action.payload || "Login failed"  ;
      })
      .addCase(loginUserWithSecureCode.pending, (state) => {
        state.status = "loading";
      })
      .addCase(loginUserWithSecureCode.fulfilled, (state, action :PayloadAction<any>) => {
        state.status = "succeeded";
        state.user = action.payload.user;
        state.token = action.payload.accessToken || null; // Store token if returned by API
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

export const { logout, setToken } = authSlice.actions;
export default authSlice.reducer;

