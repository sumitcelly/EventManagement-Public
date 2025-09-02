// src/features/auth/authSlice.js
import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import axios from "axios";

interface User {
  id: number;
  email: string;
  role: string;
  customerId: string;
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
}

let accessToken: string | null = null;

// Async login action
export const loginUser = createAsyncThunk(
  "auth/loginUser",
  async (credentials: LoginFormInputs, { rejectWithValue }) => {
    try {
      const res = await axios.post(
        "http://localhost:5220/user/login",
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
       "http://localhost:5220/user/refresh",
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

export const getAccessToken = () => accessToken;


// Async check user session
export const fetchUser = createAsyncThunk(
  "auth/fetchUser",
  async (_, { rejectWithValue }) => {
    try {
      const res = await axios.get("http://localhost:5220/api/auth/me", {
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
    },
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
      .addCase(fetchUser.fulfilled, (state, action) => {
        state.user = action.payload;
        state.isAuthenticated = !!action.payload;
      });
  },
});

export const { logout } = authSlice.actions;
export default authSlice.reducer;

