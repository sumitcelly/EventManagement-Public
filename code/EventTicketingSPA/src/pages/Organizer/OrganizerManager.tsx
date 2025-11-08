
import { TabItem, Tabs, TabsRef } from "flowbite-react";
import { HiAdjustments, HiClipboardList, HiUserCircle } from "react-icons/hi";
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

export function OrganizerManager() {
  const tabsRef = useRef<TabsRef>(null);
 // const location = useLocation();
  
  const {organizerId,mode} = useParams();

  //mode valid values are ticketlist,new,edit
  console.log('organizer id, mode from params',organizerId,mode);

 
  const [localActiveTab, setLocalActiveTab] = useState(0);
  //console.log('active tab in state is',activeTab);
  // const organizerInfo = location.state;
  // console.log('Organizer Info is ', organizerInfo);

   const { data, isLoading } = 
    useQuery(['Organizer',organizerId], async () => {
        console.log("Fetching organizer details", organizerId);
        const res = await axiosClient.get(`/eventorganizer/${organizerId}`);
        console.log('detail for organizer', res);
        //console.log('orders fetched from backend',res.data);
        // const data: OrganizerInfo = {
        //   organizerId: 1,
        //   organizerName: "Monika C",
        //   organizationName: "PDAC",
        //   organizerWebsite: "https://www.pdac.com",
        //   organizerEmail: "hello@pdac.com",
        //   organizerDescription: "Premier Dance Academy of Colorado",
        //   organizerEventBaseUrl: "pdac-events", 
        //   organizerCountry: "USA",
        //   organizerPhone: "719-555-0123",
        //   organizerInstagram: "@pdacdance",
        //   organizerFacebook: "facebook.com/pdacdance",
        //   organizerStripeAccountId: "",
        //   organizerStripeAccountStatus: "pending",
        //   organizerAboutMe:"helllo",
        //   organizerImageUrl:"/ghg/"
        // };
    
        return res.data;
      },
      {
        //staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
        //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
  
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
     else {
     // setLocalActiveTab(0);
      console.log("organizer Info mode");
      tabsRef.current?.setActiveTab(0);
    }
  }, [organizerId,mode]);

  return (  
    <Tabs aria-label="Organizer Manager" 
      ref={tabsRef}
      className="max-w-2xl mx-auto "
      variant="underline" onActiveTabChange={(tab) =>{   setLocalActiveTab(tab);console.log("active tab change called",tab);}}>
    
      <TabItem title="About Info" icon={HiUserCircle}>
        <OrganizerAbout organizerId={organizerId} organizerInfo  ={data}/>
      </TabItem>

      <TabItem   title="Connection Info" icon={HiUserCircle}  >
        <OrganizerConnect organizerId={organizerId} organizerInfo={data}/>
      </TabItem>
    </Tabs>
  );
}
