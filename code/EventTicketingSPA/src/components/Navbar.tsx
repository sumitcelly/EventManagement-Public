// src/components/Navbar.jsx
import axiosClient from "../api/axiosClient";
import { useAppDispatch ,useAppSelector} from "../app/hook";
import { logout } from "../features/auth/authSlice";
import axios from "axios";
import { Link } from "react-router-dom";  


export default function Navbar() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();

  const handleLogout = async () => {
    await axiosClient.post(
      "/user/logout",
      {},
      { withCredentials: true }
    );
    dispatch(logout());
  };

  return (
    <nav className="bg-gray-800 text-white px-4 py-3 flex justify-between items-center">
      <div className="font-bold text-lg">Ticketing App</div>
      <div className="space-x-4">
        {isAuthenticated ? (
          <>
            <span>Welcome, {user?.email} </span>
            <button
              onClick={handleLogout}
              className="bg-red-500 px-3 py-1 rounded hover:bg-red-600"
            >
              Logout
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="hover:underline">Login</Link>
          </>
        )}
      </div>
    </nav>
  );
}
