import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useNavigate,Link, useParams } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { Ticket } from "../../types/Tickets";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect } from "react";
import { TeamMember } from "../../types/Teams";

import toast, {Toaster} from "react-hot-toast";
// 


  
export default function TeamList({organizerId,isActive}: {organizerId?: string, isActive?:boolean}) {
  const navigate = useNavigate();
 
  console.log('event id from props and is active',organizerId, isActive);

  const queryClient = useQueryClient();

  const deleteUser = async (userId:number) => 
  {
    try 
    {
        console.log('Deleting member', userId);
        // 1. Optimistically update UI
        queryClient.setQueryData(['TeamByOrganizer', organizerId], (oldData: TeamMember[] | undefined) => {
        if (!oldData) return [];
        return oldData.filter(member => member.userId !== userId);
        });

        // 2. Make API call
        //await axiosClient.delete(`/events/${eventId}`);

        // 3. Invalidate to verify our optimistic update
        // This ensures our cache matches the server state
        await queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);

    } 
    catch (error) 
    {
        console.error('Failed to delete memner:', error);
    // On error, refetch to restore correct state
        await queryClient.invalidateQueries(['TeamByOrganizer', organizerId]);
    }
}

  const { data, isLoading } = 
  useQuery(['TeamByOrganizer',organizerId], async () => {
      console.log("Fetching members for organizer:", organizerId);
      //const res = await axiosClient.get(`/SalesOrder/ByUserId/${userId}`);
      //console.log('orders fetched from backend',res.data);
      let data:TeamMember[]=[];
      data.push({
        userId: 5,
        email:"fgfgfgfgfgfgfgffg@gmail.com",
        status:"Active",
        permissions:["Admin"],
        name:"member1"
      },
    {
        userId: 7,
        email:"sumit.celly@gmail.com",
        status:"Pending",
        permissions:["Checkin"],
        name:"member2"
      });
      return data;
    },
    {
      //staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      //cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes

      // refetchOnMount: false,      // don’t always re-fetch on mount
      // refetchOnWindowFocus: false,
      // refetchOnReconnect: false,
      enabled: !!organizerId && isActive //  only run query if we have an id
    }
  );



  if (isLoading) return <p>Loading...</p>;

  return (
    <div className="max-w-l mx-auto">
      <Toaster position="top-right" />
      <h2 className="text-xl font-semibold mb-4 text-center">Build your team</h2>
      {/*does not work for som reason. the useeffect on evenmanager is not triggered*/}
      
      <div className="flex flex-row mt-4">
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 mb-2 rounded hover:bg-blue-700"
                onClick={()=> navigate(`/teammanager/${organizerId}/newmember`)}
              >
                Add member
          </button> 
      </div>
    <div className="p-4 bg-white rounded-lg shadow">
    {/* Header Row */}
    <div className="grid grid-cols-1 sm:grid-cols-4 font-semibold text-gray-700 border-b pb-2 mb-2">
      <div>Email</div>
      <div>Name</div>
      <div>Status</div>
      <div>Permissions</div>
      
    </div>

    {/* Member Rows */}
    <div className="space-y-2">
      {data && data.map((member) => (
        <div
          key={member.userId}
          className="grid grid-cols-1 sm:grid-cols-4 items-center text-gray-700 bg-gray-50 rounded-lg px-3 py-2 hover:bg-gray-100 transition"
        >
          <div className="truncate pr-2" title={member.email}>{member.email}</div>
          <div>{member.name}</div>
          <div>
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
          </div>
          <div className="flex flex-row justify-between">
            <div>
              {member.permissions?.join(", ")}
            </div>
            <div onClick={(e)=>e.stopPropagation()}>
              <ListMenu
                linkData={{
                  viewLink: "",
                  editLink: `/teammanager/${organizerId}/edit`,
                  delete:()=>deleteUser(Number(member.userId)),
                  editData: member
                }}
              />
            </div>
          </div>
                
          
       
        </div>
      ))}
    </div>
  </div>

  </div>
  );
}
