import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./components/Navbar";
import LoginPage from "./pages/Login";
import MyEvents from "./pages/MyEvents";
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

import Dashboard  from "./pages/Organizer/Dashboard";
import EventForm from "./pages/Organizer/EventForm";
import TicketDashboard from "./pages/Organizer/TicketDashboad";
import TicketBasics from "./pages/Organizer/TicketBasics";
import { EventManager } from "./pages/Organizer/EventManager";

export default function App() {
  const isAuthenticated = useSelector((state :any) => state.auth.isAuthenticated);

  return (
    <BrowserRouter>
      <AppNavbar />
      <Routes>
        <Route
          path="/login"
          element={
            isAuthenticated ? <Navigate to="/myevents" /> : <LoginPage />
          }
        />

        <Route path="/EventManager/:eventId" element={isAuthenticated?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId/:mode" element={isAuthenticated?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId/:mode/:ticketId" element={isAuthenticated?<EventManager/>:<LoginPage/>}/>

        <Route path="/Dashboard" element={isAuthenticated?<Dashboard/>:<LoginPage/>}/>
           
        <Route
          path="/myevents"
          element={
            isAuthenticated ? <MyEvents /> : <Navigate to="/login" />
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
               <OrderSummary /> 
          }
        />
         <Route
          path="/orderconfirmation/event/:eventId"
          element={
               isAuthenticated ? <OrderConfirmation /> : <Navigate to="/login" />
          }
        />
        {/* Optionally, redirect unknown routes */}
          <Route
          path="/ticketdetails/:eventId/:salesOrderCode"
          element={
            isAuthenticated ? <TicketDetails/> : <Navigate to="/login" />
          }
        />
        <Route
          path="*"
          element={
            isAuthenticated ? <MyEvents/> : <Navigate to="/" />
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
