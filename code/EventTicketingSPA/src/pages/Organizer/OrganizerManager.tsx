
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle, HiCreditCard,HiUser,HiLink} from "react-icons/hi";
import { MdDashboard } from "react-icons/md";
import EventForm from "./EventForm";
import TicketDashboard from "./TicketDashboad";
import { useLocation, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import TeamList from "./TeamList";
import MemberAdd from "./MemberAdd";
import { useQuery } from "react-query";
import { OrganizerInfo } from "../../types/Organizer";
import OrganizerAbout from "./OrganizerAbout";
import OrganizerConnect from "./OrganizerConnect";
import axiosClient from "../../api/axiosClient";
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import OrganizerStripe from "./OrganizerStripe";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { Toaster } from "react-hot-toast";
import Footer from "../../components/Footer";

export function OrganizerManager() {
  const tabsRef = useRef<TabsRef>(null);
 // const location = useLocation();
  
  const {mode} = useParams<{mode: string}>();
  const user =  useAppSelector((state: RootState) => state.auth?.user);
  const role  = user?.role;
  const organizerId =  user?.customerId;

  //mode valid values are ticketlist,new,edit
  console.log('organizer id, customerId from auth',organizerId,role);
  const [localActiveTab, setLocalActiveTab] = useState(0);

   const { data, isLoading } = 
    useQuery(['Organizer',organizerId], async () => {
        console.log("Fetching organizer details", organizerId);
        const res = await axiosClient.get(`/eventorganizer/${organizerId}`);
        console.log('detail for organizer', res);
        return res.data;
      },
      {
        staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
        cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
  
        // refetchOnMount: false,      // don’t always re-fetch on mount
        // refetchOnWindowFocus: false,
        // refetchOnReconnect: false,
        enabled: !!organizerId //  only run query if we have an id
      }
    );
    
  useEffect(() => {
    if (mode === "organizerConnect")
    {
      console.log("organizer Connect mode")
      tabsRef.current?.setActiveTab(1);
    }
    else if (mode ==="stripe")
    {
     console.log("Stripe Connect mode")
      tabsRef.current?.setActiveTab(2); 
    }
     else {
     // setLocalActiveTab(0);
      console.log("organizer Info mode");
      tabsRef.current?.setActiveTab(0);
    }
  }, [organizerId,mode]);

  // if (isLoading)
  // {
  //   return (<h2>Loading...</h2>)
  // }

  return (  
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col min-h-full">
          <div>
          {role === "Attendee" &&(
            <div className="flex text-wrap text-accent-dark text-lg font-body justify-center max-w-xl mx-auto mt-4">
              We need some information from you so that you can organize events. Stripe information is manadatory for paid events.</div>
          )}
          <Tabs aria-label="Organizer Manager" 
            ref={tabsRef}
            className="max-w-2xl mx-auto "
            variant="underline" onActiveTabChange={(tab) =>{setLocalActiveTab(tab);console.log("active tab change called",tab);}}>
          
            <TabItem title="About Info" icon={HiUser}>
              <OrganizerAbout organizerId={organizerId} organizerInfo  ={data}/>
            </TabItem>

            <TabItem   title="Connection Info" icon={HiLink}  >
              <OrganizerConnect organizerId={organizerId} organizerInfo={data}/>
            </TabItem>
            <TabItem   title="Stripe Info" icon={HiCreditCard}  >
              <OrganizerStripe organizerId={organizerId} organizerInfo={data}/>
            </TabItem>
          </Tabs>
        </div>
          <Footer/>
        </div>
    </IonContent>
    </IonPage>
  );
}
