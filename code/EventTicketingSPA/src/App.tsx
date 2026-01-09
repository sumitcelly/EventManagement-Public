
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
import SalesOrderTicket from "./components/SalesOrderTicket";
import ScannerDashboard from "./pages/Scanner/Dashboard";
import ScanTicket from "./pages/Scanner/ScanTicket";
import OrderPayment from "./pages/OrderPayment";

export default function App() {
  const isAuthenticated = useSelector((state :any) => state.auth.isAuthenticated);
  //setupIonicReact();
  return (
    <IonReactRouter>
     
      <IonRouterOutlet>
        {/* No idea why adding it at beginning works. If put at end, then I always see Myevents page for 
        any route. Tooke me a day. I would expect it be other way*/}
        <Route
          path="/"
           render={() =>
            isAuthenticated ? <MyEvents/> : <IonPage><IonHeader><AppNavbar/></IonHeader></IonPage>
          }
        />
        <Route
          path="/login"
          
          render={()=> isAuthenticated ? <MyEvents/> : <LoginPage />}
        />
        <Route path="/Dashboard"  render={() =>isAuthenticated?<Dashboard/>:<LoginPage/>}/>
        <Route
          path="/myevents"
  
           render={() =>
            isAuthenticated ? <MyEvents /> :<LoginPage />
          }
        />
          <Route
         
          path="/ticketdetails/:eventId/:salesOrderCode"
           render={() =>
            isAuthenticated ? <TicketDetails/> : <LoginPage/>
          }
          />
        <Route
          path="/eventdetails/:id"
           render={() =>
            <EventDetails /> 
          }
        />
        <Route path="/Organizer/SalesOrderReport"       
          render={() =>isAuthenticated?
          <OrderReport/>:<LoginPage/>}
        />
      
        <Route
          path="/searchevents/keyword/:keyword?"
           render={() =>
            <SearchEvents/> 
          }
        /> 
         <Route
          path="/searchevents/location/:location?"
           render={() =>
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents/location/:location/keyword/:keyword"
           render={() =>
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents/keyword/:keyword/location/:location"
           render={() =>
            <SearchEvents /> 
          }
        /> 
         <Route
          path="/searchevents"
           render={() =>
            <SearchEvents /> 
          }
        /> 
        <Route path="/Auth/SendSecureCode/:returnUrl?"><SendSecureCode/></Route>
        <Route path="/Auth/ValidateSecureCode/:returnUrl?" ><ValidateSecureCode/></Route>       
        <Route path="/Signup" ><SignupForm/></Route>  

        <Route path="/EventManager"  render={() =>isAuthenticated?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId"  render={() =>isAuthenticated?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId/:mode"  render={() =>isAuthenticated?<EventManager/>:<LoginPage/>}/>
        <Route path="/EventManager/:eventId/:mode/:ticketId"  render={() =>isAuthenticated?<EventManager/>:<LoginPage/>}/>

     
        <Route path="/OrganizerManager/:organizerId/:mode?"  render={() =>isAuthenticated?<OrganizerManager/>:<LoginPage/>}/>
        <Route path="/TeamManager/:organizerId"  render={() =>isAuthenticated?<TeamManager/>:<LoginPage/>}/>
        <Route path="/TeamManager/:organizerId/:mode"  render={() =>isAuthenticated?<TeamManager/>:<LoginPage/>}/>
        
        <Route
          path="/buytickets/:id"
           render={() =>
              <BuyTickets /> 
          }
        />

        <Route
          path="/orderpayment/event/:id"
           render={() =>
               <OrderPayment /> 
          }
        />

        <Route
          path="/orderpayment"
           render={() =>
               <OrderPayment /> 
          }
        />
        {/* <Route
          path="/ordersummary/:id"
           render={() =>
               <OrderSummary /> 
          }
        /> */}
         <Route
          path="/orderconfirmation/event/:eventId"
           render={() =>
               <OrderConfirmation /> 
          }
        /> 

        <Route path="/ScannerDashboard"  render={() =>isAuthenticated?<ScannerDashboard/>:<LoginPage/>}/>
        <Route path="/ScanTicket/:eventId"  render={() =>isAuthenticated?<ScanTicket/>:<LoginPage/>}/>

        </IonRouterOutlet>
      </IonReactRouter>
    );
  }

   
    
 
