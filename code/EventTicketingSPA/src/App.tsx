import LoginPage from "./pages/Login";
import MyEvents from "./pages/MyEvents";
import TicketDetails from "./pages/TicketDetails";
import { useSelector } from "react-redux";

//import { AuthState } from "./features/auth/authSlice";
import { Reducer } from "@reduxjs/toolkit";
import AppNavbar from "./components/Navbar";
import { useAppDispatch } from "./app/hook";
import { fetchUser } from "./features/auth/authSlice";
import SearchEvents from "./pages/SearchEvents";
import EventDetails from "./pages/EventDetails";
import BuyTickets  from "./pages/BuyTickets";
import OrderSummary from "./pages/OrderSummaryDefunct";
import OrderConfirmation from "./pages/OrderConfirmation";

import Dashboard  from "./pages/Organizer/Dashboard";
import EventForm from "./pages/Organizer/EventForm";
import TicketDashboard from "./pages/Organizer/TicketDashboad";
import TicketBasics from "./pages/Organizer/TicketBasics";
import { EventManager } from "./pages/Organizer/EventManager";
import { TeamManager } from "./pages/Organizer/TeamManager";
import { OrganizerManager } from "./pages/Organizer/OrganizerManager";
import SendSecureCode from "./Auth/SendSecureCode";
import ValidateSecureCode from "./Auth/ValidateSecureCode";
import SignupForm from "./pages/SignupForm";
import OrderReport from "./pages/Organizer/OrderReport";
import { BrowserRouter,  Route, Switch } from "react-router-dom";
import { IonReactRouter } from "@ionic/react-router";
import { IonContent, IonHeader, IonPage, IonRouterOutlet,IonTab,setupIonicReact } from "@ionic/react";
//import { App as CapacitorApp } from '@capacitor/app';
import { useEffect } from 'react';
import SalesOrderTicket from "./components/SalesOrderTicket";
import ScannerDashboard from "./pages/Scanner/Dashboard";
import ScanTicket from "./pages/Scanner/ScanTicket";
import OrderPayment from "./pages/OrderPayment";
import RefundOrder from "./pages/RefundOrder";
import CampaignList from "./pages/Organizer/CampaignList";
import CampaignAdd from "./pages/Organizer/CampaignAdd";
import ResetPassword from "./pages/ResetPassword";
import { Toaster } from "react-hot-toast";

export default function App() {
  const isAuthenticated = useSelector((state :any) => state.auth.isAuthenticated);
  const role = useSelector((state :any) => state.auth?.user?.role);
  //const dispatch = useAppDispatch();
  
  // useEffect(() => {
  //   dispatch(fetchUser());
  // }, [dispatch]);
  
  const checkScannerAccess = () => {
    return isAuthenticated && role !== "Attendee";
  }

  const checkOwnerAccess = () => {
      return isAuthenticated && role === "Owner";
  }

  const checkBasicAdminAccess = () => {
    return isAuthenticated && (role === "Owner" || role === "FullAdmin" || role === "RestrictedAdmin");
  }

  const checkFullAdminAccess = () => {
    return isAuthenticated && (role === "Owner" || role === "FullAdmin");
  }

  const checkAttendeeAccess = () => {
    return isAuthenticated && role;
  }

  // Handle Capacitor back button
  // useEffect(() => {
  //   const handleBackButton = async () => {
  //     // Go back in browser history
  //     window.history.back();
  //   };

  //   CapacitorApp.addListener('backButton', handleBackButton);

  //   return () => {
  //     CapacitorApp.removeAllListeners();
  //   };
  // }, []);

  //setupIonicReact();
  return (
    <IonReactRouter>
      <Toaster position="top-center" containerStyle={{ zIndex: 99999 }}  />
      {/**causes lots of issues in web navigation, ionrouteroutlet. But maybe needed for capacitor/mobile */}
      {/* <IonRouterOutlet> */}
        {/* No idea why adding it at beginning works. If put at end, then I always see Myevents page for 
        any route. Tooke me a day. I would expect it be other way*/}
        <Route
          exact
          path="/"
           render={() =>
           {
              if (isAuthenticated && checkBasicAdminAccess())
                return <Dashboard />;
              else if (isAuthenticated)
                return <MyEvents />;
              else
                return <LoginPage />;
           }
          }
        />
      
        <Route
          path="/login"
           render={() =>
           {
              if (isAuthenticated && checkBasicAdminAccess())
                return <Dashboard />;
              else if (isAuthenticated)
                return <MyEvents />;
              else
                return <LoginPage />;
           }
          } 
        />
        <Route path="/Dashboard"  render={() =>checkBasicAdminAccess()?<Dashboard/>:<LoginPage/>}/>
        <Route
          path="/myevents"
  
           render={() =>
            isAuthenticated ? <MyEvents /> :<LoginPage />
          }
        />
       
         <Route   
         exact    
        
          path="/ticketdetails"
           render={() =>
            isAuthenticated ? <TicketDetails/> : <LoginPage/>
          }
          /> 
           <Route       

          path="/ticketdetails/:encryptedOrderId"
           render={() =>
            <TicketDetails/> 
          }
          /> 
        <Route
          path="/emailcampaigns"
           render={() => checkBasicAdminAccess()?
            <CampaignList /> : <LoginPage />
          }
        />
        <Route
          path="/managecampaign"
           render={() => checkBasicAdminAccess()?
            <CampaignAdd />  : <LoginPage />
          }
        />

          <Route
          exact
        
            path="/eventdetails/:customerName/:eventName"
            render={() =>
              <EventDetails /> 
            }
          />
   
         <Route
          path="/refundorder"
           render={() => isAuthenticated? <RefundOrder /> : <LoginPage />
          }
        />
        <Route path="/Organizer/SalesOrderReport"       
          render={() =>checkFullAdminAccess()?
          <OrderReport/>:<LoginPage/>}
        />
      
        <Route
          exact
         
          path="/searchevents"
           render={() =>
            <SearchEvents  /> 
          }
        /> 

        <Route path="/Auth/SendSecureCode/:returnUrl?"><SendSecureCode/></Route>
        <Route path="/Auth/ValidateSecureCode/:returnUrl?" ><ValidateSecureCode/></Route>       
        <Route path="/Signup" render={() =>isAuthenticated?<SignupForm/>:<LoginPage/>}/>
        <Route path="/ResetPassword"  render={() =>isAuthenticated?<ResetPassword/>:<LoginPage/>}/>
        
        {/* <Route path="/EventManager"  render={() =>checkBasicAdminAccess()?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId"  render={() =>checkBasicAdminAccess()?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId/:mode"  render={() =>checkBasicAdminAccess()?<EventManager/>:<LoginPage/>}/> */}
        <Route path="/EventManager/:eventId?/:mode?/:ticketId?"  render={() =>checkBasicAdminAccess()?<EventManager/>:<LoginPage/>}/>

     
        <Route path="/OrganizerManager/:mode?"  render={() => isAuthenticated?<OrganizerManager/>:<LoginPage/>}/>
        <Route path="/TeamManager"  render={() =>checkBasicAdminAccess()?<TeamManager/>:<LoginPage/>}/>
        {/* <Route path="/TeamManager/:organizerId/:mode"  render={() =>isAuthenticated?<TeamManager/>:<LoginPage/>}/>
         */}
        <Route
        
          path="/buytickets/:id"
           render={() =>
              <BuyTickets /> 
          }
        />

        <Route
       
          path="/orderpayment/event/:id"
           render={() => isAuthenticated ? <OrderPayment /> : <LoginPage />}
        />
         {/* <Route
          path="/orderpayment"
           render={() => isAuthenticated ? <OrderPayment /> : <LoginPage />}
        /> */}

         <Route
           key="/orderconfirmation"
          path="/orderconfirmation/event/:eventId"
           render={() => isAuthenticated ? <OrderConfirmation /> : <LoginPage />}
        />

        <Route path="/ScannerDashboard"  render={() =>checkScannerAccess()?<ScannerDashboard/>:<LoginPage/>}/>
        <Route path="/ScanTicket/:eventId"  render={() =>checkScannerAccess()?<ScanTicket/>:<LoginPage/>}/>

        {/* </IonRouterOutlet> */}
      </IonReactRouter>
    );
  }

   
    
 
