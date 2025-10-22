
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TicketBasics from "./TicketBasics";
import EventPublish from "./EventPublish";

export function EventManager() {
  const tabsRef = useRef<TabsRef>(null);

  const {eventId,mode,ticketId} = useParams();

  //mode valid values are ticketlist,new,edit
  console.log('event, ticket id ,mode from params',eventId,ticketId,mode);

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [activeTab, setActiveTab] = useState((ticketId  || mode) ? 1:0);
  console.log('active tab in state is',activeTab);
  

  useEffect(() => {
    if (ticketId || mode) {
      // Switch to 2nd tab programmatically
      tabsRef.current?.setActiveTab(1);
    } else {
      tabsRef.current?.setActiveTab(0);
    }
  }, [ticketId, mode]);

  return (  
    <Tabs aria-label="Event Manager" 
      ref={tabsRef}
      className="max-w-2xl mx-auto "
      variant="underline" onActiveTabChange={(tab) =>{ console.log("active tab change called",tab); setActiveTab(tab)}}>
    
      <TabItem title="Event Details" icon={HiUserCircle}>
        <EventForm id={eventId} isActive={activeTab===0}/>
      </TabItem>

      <TabItem  title="Ticket(s)" icon={MdDashboard}>
        {
          mode === "ticketlist" || !mode
          ?<TicketDashboard eventId={eventId} isActive={activeTab===1}/>
          :<TicketBasics eventId={eventId} ticketId={ticketId}/>         
        }
      </TabItem>

      <TabItem   title="Go Online!" icon={HiUserCircle} disabled={eventId ==null}>
        <EventPublish eventId={eventId}/>
      </TabItem>
    </Tabs>
  );
}
