
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TicketBasics from "./TicketBasics";
import EventPublish from "./EventPublish";
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";

interface EventManagerParams {
  eventId?: string;
  mode?: string;
  ticketId?: string;
}

export function EventManager() {
  const tabsRef = useRef<TabsRef>(null);

  const {eventId,mode,ticketId} = useParams<EventManagerParams>();

  //mode valid values are ticketlist,new,edit
  console.log('event, ticket id ,mode from params',eventId,ticketId,mode);

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  //console.log('active tab in state is',activeTab);

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
  }, [mode,ticketId]);

  return (  
    <IonPage>
      <IonHeader>
          <AppNavbar />
       </IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
    <Tabs aria-label="Event Manager" 
      ref={tabsRef}
      className="max-w-2xl mx-auto "
      variant="underline" onActiveTabChange={(tab) =>{   setLocalActiveTab(tab);console.log("active tab change called",tab);}}>
    
      <TabItem title="Event Details" icon={HiUserCircle}>
        <EventForm id={eventId} isActive={localActiveTab===0}/>
      </TabItem>

      <TabItem  title="Ticket(s)" icon={MdDashboard} disabled={eventId == null}>
        {
          (mode === "ticketlist" || !mode)
          ?<TicketDashboard eventId={eventId} isActive={localActiveTab===1}/>
          :<TicketBasics eventId={eventId} ticketId={ticketId}/>         
        }
      </TabItem>

      <TabItem  title="Go Live!" icon={HiUserCircle} disabled={eventId ==null}>
        <EventPublish eventId={eventId}/>
      </TabItem>
    </Tabs>
    </IonContent>
    </IonPage>
  );
}
