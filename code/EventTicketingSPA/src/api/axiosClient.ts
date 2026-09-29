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
      // const requestUrl = originalRequest?.url ?? "";
      // const isAuthEndpoint = /\/user\/(login|refresh|logout|VerifyEmailCode)|\/api\/auth\//.test(requestUrl);

      // if (isAuthEndpoint) {
      //   return Promise.reject(error);
      // }

      originalRequest._retry = true;

      // Serialize refresh calls so only one refresh runs at a time.
      // Other requests awaiting refresh will reuse the same promise.
      if (!(axiosClient as any)._refreshPromise) {
        (axiosClient as any)._refreshPromise = refreshAccessToken().then((t) => {
          (axiosClient as any)._refreshPromise = null;
          return t;
        });
      }

      const newToken = await (axiosClient as any)._refreshPromise;
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

