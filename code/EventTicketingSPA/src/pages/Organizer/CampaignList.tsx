import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link, useParams } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { Ticket } from "../../types/Tickets";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect, useState } from "react";
import { TeamMember } from "../../types/Teams";

import toast, {Toaster} from "react-hot-toast";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import  Footer  from "../../components/Footer";
import { RichTextEditorModal } from "../../components/RichTextEditorModal";
// 


  
export default function CampaignList() {
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);
  const [previewContent, setPreviewContent] = useState("");
  
  const history = useHistory();
  const  user = useAppSelector((state:RootState) => state.auth);

  const organizerId = user.user?.customerId;
  console.log('Organizer id is ', user.user?.customerId);

  const queryClient = useQueryClient();

  const deleteCampaign = async (campaignId:number) => 
  {
    try 
    {
        console.log('Deleting campaign', campaignId);
        // 1. Optimistically update UI
        queryClient.setQueryData(['CampaignByOrganizer', organizerId], (oldData: any) => {
        if (!oldData) return [];
        return oldData.filter((campaign: any) => campaign.campaignId !== campaignId);
        });

        // 2. Make API call
        await axiosClient.delete(`/EmailCampaign/${campaignId}`, { headers: {
                  'Content-Type': 'application/json'}
                 },).then(response => {
          console.log('Delete successful:', response.data);
          toast.success("Campaign deleted succefully.");
        })
      .catch(error => {
          console.error('Error deleting item:', error);
           toast.error("Error deleting user.");
        });

        // 3. Invalidate to verify our optimistic update
        // This ensures our cache matches the server state
        await queryClient.invalidateQueries(['CampaignByOrganizer', organizerId]);

    } 
    catch (error) 
    {
        console.error('Failed to delete campaign:', error);
    // On error, refetch to restore correct state
        await queryClient.invalidateQueries(['CampaignByOrganizer', organizerId]);
    }
}

  const { data, isLoading } = 
  useQuery(['CampaignByOrganizer',organizerId], async () => {
     
      const res = await axiosClient.get(`/EmailCampaign/${organizerId}`);

      console.log('Campaign fetched from backend',res.data);
      
      let  campaigns:any=[];
      if (res.data)
      {
        res.data.map((temp:any)=>{
            campaigns.push({
              id:temp.id,
              eventId: temp.eventId,
              templateId:temp.templateId,
              name:temp.name,
              isDefault: temp.isDefault,
              eventName: temp.eventName,
              sendAt: temp.sendAt,
              status: temp.status
            });
        });
      }
      return campaigns;
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



  if (isLoading) return <p>Loading...</p>;

  async function  previewCampaign(campaignData: any) {
   // throw new Error("Function not implemented.");
   try {    
        const result = await axiosClient.post(`/EmailCampaign/Resolve/${campaignData.eventId}`, {
        templateId: campaignData?.templateId
        });
        console.log('Preview result from backend', result.data);
        if (result && result.status === 200) {
          console.log('Decoded preview content', atob(result.data));
          setPreviewContent(atob(result.data));
          setIsPreviewModalOpen(true);
        }
      } 
      catch (error) {
        console.error('Error previewing template:', error);
        toast.error("Error previewing email template");
    }
  }

  return (
    <IonPage>
      <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent>
        <div className="flex flex-col min-h-full">
          <div className="max-w-3xl w-full mx-auto p-3 border border-gray-300 rounded-lg shadow-lg bg-brand-neutral">
          <Toaster position="top-right" />
          <h2 className="text-xl font-semibold mb-4 text-center">Email Campaigns</h2>
      
          <div className="flex flex-row mt-4">
              <button
                    className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 mb-2 rounded hover:bg-blue-700"
                    onClick={()=> history.push(`/ManageCampaign`)}
                  >
                    New Campaign
              </button> 
          </div>
          <div className="p-4 bg-white rounded-lg shadow">
            {/* Header Row */}
            <div className="grid grid-cols-1 sm:grid-cols-5 font-semibold text-gray-700 border-b pb-2 mb-2">
              <div>Event Name</div>
              <div>Campaign</div>
              <div>SendAt</div>
              <div>Status</div>      
              <div>Action</div>            
            </div>

            {/* Member Rows */}
            <div className="space-y-2">
              {data && data.map((campaign : any) => (
                <div
                  key={campaign.id}
                  className="grid grid-cols-1 sm:grid-cols-4 items-center text-gray-700 bg-gray-50 rounded-lg px-3 py-2 hover:bg-gray-100 transition"
                >
                  <div className="truncate pr-2" title={campaign.eventName}>{campaign.eventName}</div>
                  <div className="truncate pr-2" title={campaign.name}>{campaign.name}</div>
                  <div>{new Date(campaign.sendAt).toLocaleDateString()}</div>
                  
                  {/* <div>
                    <span
                      className={`px-2 py-1 text-xs rounded-full ${
                        member.status === "Active"
                          ? "bg-green-100 text-green-700"
                          : member.status === "Pending"
                          ? "bg-yellow-100 text-yellow-700"
                          : "bg-gray-200 text-gray-700"
                      }`}
                    >
                      {member.status}
                    </span>
                  </div> */}
                  <div className="flex flex-row justify-between">
                    <div>{campaign.status}</div>
                    <div onClick={(e)=>e.stopPropagation()}>
                      <ListMenu
                        linkData={{
                          viewLink: "",
                          editLink:!campaign.isDefault? `/ManageCampaign`: '',
                          delete:()=>deleteCampaign(Number(campaign.id)),
                          editData: campaign,
                          previewData: campaign.isDefault ? () => { previewCampaign(campaign); } : undefined
                        }}
                      />
                    </div>
                  </div>
                </div>
      ))}
    </div>
    
  </div>
    <RichTextEditorModal
          modalTitle="Email Preview"
          openModal={isPreviewModalOpen}
          onClose={() => setIsPreviewModalOpen(false)}
          onConfirm={() => setIsPreviewModalOpen(false)}
          initialContent={previewContent}
        />
  </div>
      <Footer/>
  </div>
  </IonContent>
  </IonPage>);
}
