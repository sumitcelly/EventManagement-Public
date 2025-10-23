
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useLocation, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TeamList from "./TeamList";
import MemberAdd from "./MemberAdd";

export function TeamManager() {
  const tabsRef = useRef<TabsRef>(null);
  const location = useLocation();

  const {organizerId,mode} = useParams();

  //mode valid values are ticketlist,new,edit
  console.log('organizer id, mode from params',organizerId),mode;

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  //console.log('active tab in state is',activeTab);
  let memberInfo = location.state;
  
  // if (!memberInfo)
  // {
  //   memberInfo ={"userId":userId};
  // }

  useEffect(() => {
    if (mode === "newuser" || mode ==="edituser")
    {
      console.log("new user creation")
      tabsRef.current?.setActiveTab(1);
    }
     else {
     // setLocalActiveTab(0);
      console.log("list tab");
      tabsRef.current?.setActiveTab(0);
    }
  }, [mode, organizerId]);

  return (  
    <Tabs aria-label="Team Manager" 
      ref={tabsRef}
      className="max-w-2xl mx-auto "
      variant="underline" onActiveTabChange={(tab) =>{   setLocalActiveTab(tab);console.log("active tab change called",tab);}}>
    
      <TabItem title="Team List" icon={HiUserCircle}>
        <TeamList organizerId={organizerId} isActive={localActiveTab===0}/>
      </TabItem>

      <TabItem   title="Add Member" icon={HiUserCircle}  >
        <MemberAdd organizerId={organizerId} memberInfo={memberInfo}/>
      </TabItem>
    </Tabs>
  );
}
