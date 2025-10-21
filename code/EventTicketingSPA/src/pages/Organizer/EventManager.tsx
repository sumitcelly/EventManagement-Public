
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useParams } from "react-router-dom";
import { useRef, useState } from "react";
import TicketBasics from "./TicketBasics";

export function EventManager() {
  const tabsRef = useRef<TabsRef>(null);
  
  const {eventId,mode,ticketId} = useParams();

  //mode valid values are ticketlist,new,edit
  console.log('event, ticket id ,mode from params',eventId,ticketId,mode);

  //if we have ticket id or mode, means we are on the ticket tab
  const [activeTab, setActiveTab] = useState(ticketId  || mode? 1:0);
  console.log('active tab is',activeTab);
  
  console.log("Event ID in EventManager:", eventId);
  return (  
    <Tabs aria-label="Tabs with icons" 
    
      className="max-w-2xl mx-auto p-6 space-y-6"
      variant="underline" onActiveTabChange={(tab) => setActiveTab(tab)}>
      
      <TabItem active ={activeTab===0} title="Event Details" icon={HiUserCircle}>
        <EventForm id={eventId}/>
      </TabItem>

      <TabItem active ={activeTab===1} title="Ticket(s)" icon={MdDashboard}>
        {
          mode === "ticketlist" || !mode
          ?<TicketDashboard eventId={eventId} isActive={activeTab===1}/>
          :<TicketBasics eventId={eventId} ticketId={ticketId}/>         
        }
      </TabItem>
    </Tabs>
  );
}
