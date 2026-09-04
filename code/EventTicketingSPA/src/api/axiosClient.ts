import axios from "axios";
import { refreshAccessToken, getAccessToken, logout } from "../features/auth/authSlice";
import { store } from "../app/store";
import { clearAuthQueryCache } from "../queryClient";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // important for cookies
});



axiosClient.interceptors.request.use(
  async (config) => {
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: auto-refresh on 401
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If 401 and not already retried
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const newToken = await refreshAccessToken();
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return axiosClient(originalRequest);
      } else {
        // Refresh failed → logout and clear all cached data
        clearAuthQueryCache();
        store.dispatch(logout());
      }
    }

    return Promise.reject(error);
  }
);

export default axiosClient;

