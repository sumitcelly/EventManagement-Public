
import {  TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiOutlineLightningBolt, HiOutlineTicket, HiUserCircle, HiUserGroup } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useLocation, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TicketBasics from "./TicketBasics";
import EventPublish from "./EventPublish";
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import Footer from "../../components/Footer"
import { useQuery } from "react-query";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import axiosClient from "../../api/axiosClient";

interface EventManagerParams {
  eventId?: string;
  mode?: string;
  ticketId?: string;
}

export function EventManager() {
  
  const tabsRef = useRef<TabsRef>(null);
  const user = useAppSelector((state: RootState) => state.auth.user);
  const customerUrlName = user?.customerUrlName || "";
  const {eventId,mode,ticketId} = useLocation().state as EventManagerParams || {};

  //mode valid values are ticketlist,new,edit
  console.log('event, ticket id ,mode from params',eventId,ticketId,mode);

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  

  useEffect(() => {
    if (mode === "publish")
    {
      console.log("tring to publish")
      tabsRef.current?.setActiveTab(2);
    }
    else if (ticketId || (mode ==="new" || mode ==="ticketlist")) 
    {
      // Switch to 2nd tab programmatically
      //setLocalActiveTab(1);
      console.log("ticket list tab");
      tabsRef.current?.setActiveTab(1);
    } else {
     // setLocalActiveTab(0);
      console.log("event tab");
      tabsRef.current?.setActiveTab(0);
    }
  }, [mode,ticketId,eventId]);

  
  return (  
    <IonPage>
      <IonHeader>
          <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col  min-h-full">
        <div>
          <Tabs aria-label="Event Manager" 
            ref={tabsRef}
            className="max-w-2xl mx-auto "
            variant="underline" onActiveTabChange={(tab) =>{setLocalActiveTab(tab);console.log("active tab change called",tab);}}>
          
            <TabItem title="Event Details" icon={HiUserGroup}>
              <EventForm id={eventId}  organizerEventBaseUrl={customerUrlName} isActive={localActiveTab===0}/>
            </TabItem>

            <TabItem  title="Ticket(s)" icon={HiOutlineTicket} disabled={eventId == null}>
              {
                (mode === "ticketlist" || mode ==="" || !mode)
                ?<TicketDashboard eventId={eventId} isActive={localActiveTab===1}/>
                :<TicketBasics eventId={eventId} ticketId={ticketId} mode={mode}  key={mode === "new" ? crypto.randomUUID() : ticketId} />         
              }
            </TabItem>

            <TabItem  title="Go Live!" icon={HiOutlineLightningBolt} disabled={eventId ==null}>
              <EventPublish eventId={eventId} isActive={localActiveTab===2}/>
            </TabItem>
          </Tabs>
        </div>
        <Footer/>
      </div>
    </IonContent>
    </IonPage>
  );
}
