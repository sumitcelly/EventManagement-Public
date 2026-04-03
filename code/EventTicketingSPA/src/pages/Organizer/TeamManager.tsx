
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
import AppNavbar from "../../components/Navbar";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import Footer from "../../components/Footer";

interface TeamManagerParams {
  organizerId?: string;
  mode?: string;
}

export function TeamManager() {
  const tabsRef = useRef<TabsRef>(null);
  const location = useLocation();
  const history = useHistory();
  const customerId = useAppSelector((state:RootState) => state.auth.user)?.customerId;
  // const {organizerId, mode} = useParams<TeamManagerParams>();

  // //mode valid values are ticketlist,new,edit
  // 

  //if we have ticket id or mode, means we are on the ticket tab
 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  //console.log('active tab in state is',activeTab);
  let memberInfo:any = location.state;
  console.log('Member Info is ', memberInfo);
  const organizerId = memberInfo?.organizerId ||  customerId;
  const mode= memberInfo?.mode;
  console.log('organizer id, mode from params',organizerId,mode);

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
  }, [mode, organizerId, memberInfo]);

 

  return (  
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col min-h-full">
        <div>
    
        <Tabs aria-label="Team Manager" 
          ref={tabsRef}
          className="max-w-2xl mx-auto "
          variant="underline" onActiveTabChange={(tab) =>{
                                            setLocalActiveTab(tab);
                                            console.log("active tab change called",tab);
                                            }}>
        
          <TabItem title="Team List" icon={HiUserCircle} onClick={(e)=>{e.preventDefault();history.push(`/teammanager/${organizerId}`);}}>
            <TeamList organizerId={organizerId} isActive={localActiveTab===0}/>
          </TabItem>

          <TabItem   title="Add/Update Member" icon={HiUserCircle} onClick={(e)=>{e.preventDefault();history.push(`/teammanager/${mode}/${organizerId}`, {state:memberInfo})}} >
            <MemberAdd organizerId={organizerId} memberInfo={memberInfo}/>
          </TabItem>
        </Tabs>
      </div>
        <Footer/>
      </div>
    </IonContent>
    </IonPage>
  );
}
