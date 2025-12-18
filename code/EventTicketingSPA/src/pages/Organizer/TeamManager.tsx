
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useLocation, useHistory, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TeamList from "./TeamList";
import MemberAdd from "./MemberAdd";
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";

interface TeamManagerParams {
  organizerId?: string;
  mode?: string;
}

export function TeamManager() {
  const tabsRef = useRef<TabsRef>(null);
  const location = useLocation();
  const history = useHistory();
  const {organizerId, mode} = useParams<TeamManagerParams>();

  //mode valid values are ticketlist,new,edit
  console.log('organizer id, mode from params',organizerId),mode;

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  //console.log('active tab in state is',activeTab);
  let memberInfo:any = location.state;
  console.log('Member Info is ', memberInfo);

//  useEffect(()=>{
//       tabsRef.current?.setActiveTab(localActiveTab);
//   },[localActiveTab]);

  useEffect(() => {
    if (mode === "newmember" || mode ==="edit")
    {
      console.log("new user creation/updation");
     
      tabsRef.current?.setActiveTab(1);
    }
     else {
     // setLocalActiveTab(0);
      console.log("list tab");
      tabsRef.current?.setActiveTab(0);
    }
  }, [mode, organizerId]);

 

  return (  
    <IonPage>
          <IonHeader>
            <AppNavbar />
          </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
    <Tabs aria-label="Team Manager" 
      ref={tabsRef}
      className="max-w-2xl mx-auto "
      variant="underline" onActiveTabChange={(tab) =>{
                                        setLocalActiveTab(tab);
                                        if (tab==0)
                                        {
                                           window.history.pushState({}, "", `/teammanager/${organizerId}`);
                                        }
                                        if (tab ===1 && mode)
                                        {
                                            window.history.pushState({}, "", `/teammanager/${organizerId}/${mode}`);
                                        }
                                        if (tab ===1 && !mode)
                                        {
                                            window.history.pushState({}, "", `/teammanager/${organizerId}/newmember`);
                                        }
                                        console.log("active tab change called",tab);
                                        }}>
    
      <TabItem title="Team List" icon={HiUserCircle} onClick={(e)=>{e.preventDefault();history.push(`/teammanager/${organizerId}`);}}>
        <TeamList organizerId={organizerId} isActive={localActiveTab===0}/>
      </TabItem>

      <TabItem   title="Add/Update Member" icon={HiUserCircle} onClick={(e)=>{e.preventDefault();history.push(`/teammanager/${mode}/${organizerId}`, {state:memberInfo})}} >
        <MemberAdd organizerId={organizerId} memberInfo={memberInfo}/>
      </TabItem>
    </Tabs>
    </IonContent>
    </IonPage>
  );
}
