import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./components/Navbar";
import LoginPage from "./pages/Login";
import EventsPage from "./pages/EventsPage";
import TicketDetails from "./pages/TicketDetails";
import { useSelector } from "react-redux";

//import { AuthState } from "./features/auth/authSlice";
import { Reducer } from "@reduxjs/toolkit";
import AppNavbar from "./components/Navbarnew";
import SearchEvents from "./pages/SearchEvents";
import EventDetails from "./pages/EventDetails";
import BuyTickets  from "./pages/BuyTickets";
import OrderSummary from "./pages/OrderSummary";
import OrderConfirmation from "./pages/OrderConfirmation";

export default function App() {
  const isAuthenticated = useSelector((state :any) => state.auth.isAuthenticated);

  return (
    <BrowserRouter>
      <AppNavbar />
      <Routes>
        <Route
          path="/login"
          element={
            isAuthenticated ? <Navigate to="/events" /> : <LoginPage />
          }
        />
        <Route
          path="/myevents"
          element={
            isAuthenticated ? <EventsPage /> : <Navigate to="/login" />
          }
        />
        <Route
          path="/searchevents/keyword/:keyword?"
          element={
            <SearchEvents/> 
          }
        /> 
         <Route
          path="/searchevents/location/:location?"
          element={
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents/location/:location/keyword/:keyword"
          element={
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents/keyword/:keyword/location/:location"
          element={
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents"
          element={
            <SearchEvents /> 
          }
        /> 
        <Route
          path="/eventDetails/:id"
          element={
            <EventDetails /> 
          }
        />
        <Route
          path="/buytickets/:id"
          element={
              <BuyTickets /> 
          }
        />

        <Route
          path="/ordersummary/:id"
          element={
               isAuthenticated ? <OrderSummary /> : <Navigate to="/login" />
          }
        />
         <Route
          path="/orderconfirmation/event/:eventId/salesOrderCode/:salesOrderCode"
          element={
               isAuthenticated ? <OrderConfirmation /> : <Navigate to="/login" />
          }
        />
        {/* Optionally, redirect unknown routes */}
          <Route
          path="/ticketdetails/:eventId"
          element={
            isAuthenticated ? <TicketDetails/> : <Navigate to="/login" />
          }
        />
        <Route
          path="*"
          element={
            isAuthenticated ? <Navigate to="/myevents" /> : <Navigate to="/" />
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
