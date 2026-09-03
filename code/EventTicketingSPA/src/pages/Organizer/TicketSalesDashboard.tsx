import React, { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import toast, { Toaster } from "react-hot-toast";
import axiosClient, { API_BASE_URL } from "../../api/axiosClient";
import { useAppSelector } from '../../app/hook';
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from '../../components/Navbar';
import { useEditor } from '@tiptap/react';
import { useEventItemTypes } from '../../utils/EventItemTypesQuery';
import Footer from '../../components/Footer';

// --- 1. Split DTO Interfaces (Mirroring your 3 .NET Controllers) ---

// Endpoint A: GET /api/events/{id}/details
interface EventDetailsDTO {
  eventId: number;
  eventName: string;
  ticketFeeMode: number;
  eventDate: Date;
}

interface EventItemTypeData {
   eventItemTypeId: number;
   name: string;
   description: string;
   cost: number;
   totalAllowed: number;
   ticketsSold: number;
   addOn: boolean;

}

// Endpoint B: GET /api/events/{id}/velocity
interface EventVelocityDTO {
  trend: number[]; // Sparkline points
  velocity24h: number;
  velocity7dAvg: number;
}

// Endpoint C: GET /api/events/{id}/checkins
interface EventCheckInDTO {
  checkedIn: number;
  totalSold: number;
}


// --- 2. Mock API Layer (Simulating Network Latency) ---

const MOCK_DELAY = (ms: number) => new Promise(res => setTimeout(res, ms));

const fetchEventList = async (customerId: number) : Promise<EventDetailsDTO[]> =>{
  try
  {
    const res = await axiosClient.get(`/Events/ByCustomer/${customerId}/true/true`);
    if (res?.data && res.status===200)
    {
        console.log('events fetched from backend',res.data);
        
        return res.data;
    }
    else
    {
        console.log('events fetched from backend',res.data);
        return [];
    }
  }
  catch(error)
  {
      console.error("Error fetching events:", error);
      toast.error("Error loading report data");
      return [];
  }
}

const fetchVelocity = async (eventId: number | undefined): Promise<EventVelocityDTO> => {
  if (!eventId) {
    console.warn("fetchVelocity called without a valid eventId.");
    return { trend: [0,0,0,0,0,0,0], velocity24h: 0, velocity7dAvg: 0 };
  }

  try {
    const res = await axiosClient.get<EventVelocityDTO>(`/Ticket/TicketVelocity/${eventId}`);
    if (res.status === 200 && res.data) {
      console.log('Velocity data fetched from backend:', res.data);
      return res.data;
    }
    return { trend: [0,0,0,0,0,0,0], velocity24h: 0, velocity7dAvg: 0 };
  } catch(error) {
    console.error("Error fetching velocity data:", error);
    toast.error("Error loading velocity data");
    return { trend: [0,0,0,0,0,0,0], velocity24h: 0, velocity7dAvg: 0 };
  } 
};


const fetchCheckIns = async (eventId: number | undefined): Promise<EventCheckInDTO> => {
    // 2. Guard Clause: Don't hit the network if the ID is missing
    if (!eventId) {
        console.warn("fetchCheckIns called without a valid eventId.");
        return { checkedIn: 0, totalSold: 0 };
    }

    try {
        // 3. Inform Axios what type of data to expect from the server (<EventCheckInDTO>)
        const res = await axiosClient.get<EventCheckInDTO>(`/Ticket/TicketStatusCounts/${eventId}`);
        
        if (res.status === 200 && res.data) {
            console.log('Check-in data fetched from backend:', res.data);
            return res.data;
        }
        return { checkedIn: 0, totalSold: 0 };
    } catch(error) {
        console.error("Error fetching check-in data:", error);
        toast.error("Error loading check-in data");
        return { checkedIn: 0, totalSold: 0 };
    }
}



// --- 3. Reusable UI Components ---

// A. Loading Spinner / Skeleton Wrapper
const LoadingSection = ({ className = "h-32" }: { className?: string }) => (
  <div className={`w-full bg-white rounded-xl shadow-sm border border-slate-200 p-6 flex flex-col justify-center items-center animate-pulse ${className}`}>
    <div className="w-8 h-8 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin mb-3"></div>
    <div className="h-2 w-24 bg-slate-100 rounded"></div>
  </div>
);

// B. Sparkline (Same as before)
const Sparkline = ({ data, color }: { data: number[], color: string }) => {
  const max = Math.max(...data, 1);
  const min = Math.min(...data);
  const points = data.map((d, i) => 
    `${(i / (data.length - 1)) * 100},${100 - ((d - min) / (max - min || 1)) * 100}`
  ).join(" ");

  return (
    <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="w-full h-full overflow-visible">
      <polyline points={points} fill="none" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className={color} />
    </svg>
  );
};

// C. Donut Chart (Same as before)
const CheckInDonut = ({ current, total }: { current: number, total: number }) => {
  const percent = Math.round((current / total) * 100) || 0;
  const radius = 18;
  const dash = 2 * Math.PI * radius;
  const offset = dash - (percent / 100) * dash;
  return (
    <div className="relative w-12 h-12 flex items-center justify-center">
      <svg className="-rotate-90 w-full h-full">
        <circle r={radius} cx="24" cy="24" strokeWidth="4" fill="transparent" className="stroke-slate-100" />
        <circle r={radius} cx="24" cy="24" strokeWidth="4" fill="transparent" 
          strokeDasharray={dash} strokeDashoffset={offset} className="stroke-blue-600 transition-all duration-1000" />
      </svg>
      <span className="absolute text-[10px] font-bold text-slate-700">{percent}%</span>
    </div>
  );
};

// --- 4. Main Dashboard Component ---

export default function TicketSalesDashboard() {
  const [selectedEventData, setSelectedEventData] = useState<EventDetailsDTO | null>(null);

  const user = useAppSelector((state) => state.auth.user);
  const { eventItemTypeData, eventItemTypesLoading: detailsQueryFetching } = useEventItemTypes(selectedEventData?.eventId);
  console.log(`Data for event item type is ${eventItemTypeData}`);

  const detailsQueryData: EventItemTypeData[] = Array.isArray(eventItemTypeData)
  ? eventItemTypeData
  : [];
  console.log("eventItemTypeData:", detailsQueryData);
  console.log("isArray:", Array.isArray(eventItemTypeData));
  console.log(`selected event date is ${selectedEventData?.eventDate}`);

  
  console.log(`customer id is ${user?.customerId}`);
  // --- React Query Hooks (Parallel Fetching) ---
  console.log(`selected event id is ${selectedEventData?.eventId}`);

  const eventListQuery = useQuery(
    ['event-list', user?.customerId],
    async () => fetchEventList(Number(user?.customerId)),
    {
      enabled: !!user?.customerId,
      staleTime: 1000 * 60 * 5,
      cacheTime: 1000 * 60 * 5,
      refetchOnWindowFocus: false,
    }
  );

  // 2. Velocity Data
  const velocityQuery = useQuery(
    ['event-velocity', selectedEventData?.eventId],
    () => fetchVelocity(selectedEventData?.eventId),
    {
      enabled: !!selectedEventData?.eventId,
      staleTime: 1000 * 60 * 5,
      cacheTime: 1000 * 60 * 5,
    }
  );

  // 3. Check-in Data
    const selectedDate = selectedEventData?.eventDate ? new Date(selectedEventData.eventDate) : null;

    const checkInQuery = useQuery(
      ['event-checkins', selectedEventData?.eventId],
      () => fetchCheckIns(selectedEventData?.eventId),
      {
        enabled: !!selectedEventData?.eventId && !!selectedDate && selectedDate <= new Date(),
        staleTime: 1000 * 60 * 5,
        cacheTime: 1000 * 60 * 5,
      }
    );
  
  useEffect(() => {
    if (!eventListQuery.data?.length) return;

    setSelectedEventData((prev) => {
      if (prev && eventListQuery.data.some(e => e.eventId === prev.eventId)) {
        return prev;
      }
      return eventListQuery.data[0];
    });
  }, [eventListQuery.data]);

  const calculateStripeCharges = (totalRevenue: number, totalTickets: number)=>{
    console.log(`total revener ${totalRevenue} and total tickets ${totalTickets}`);
    //get stripe values from backend.
    return 0.30 * totalTickets+ 0.029*totalRevenue; 
  }
  // Derived Calculations (Only when Details are ready)
  const financialSummary = React.useMemo(() => {
    if (!detailsQueryData.length) return null;
    console.log(`details in memory are ${detailsQueryData[0]}`);
    const totalPaidTickets = detailsQueryData.filter(i => i.cost > 0).reduce((acc, t) => acc + (t.ticketsSold), 0);
    const totalGross = detailsQueryData.reduce((acc, t) => acc + (t.ticketsSold * t.cost), 0);
    const stripeFees = selectedEventData?.ticketFeeMode === 2 ? calculateStripeCharges(totalGross, totalPaidTickets) : 0;
    return { gross: totalGross, net: (totalGross - stripeFees).toFixed(2), fees: stripeFees };
  }, [detailsQueryData, selectedEventData?.ticketFeeMode]);

  if (!user?.customerId) {
    return <LoadingSection />;
  }
  if (eventListQuery.isLoading) {
    return <LoadingSection />;
  }

  


  return (
     <IonPage>
        <IonHeader>
          <AppNavbar />
        </IonHeader>
        <IonContent>
        <div className="max-w-6xl mx-auto p-6 space-y-8 bg-slate-50 min-h-screen font-sans text-slate-800">
      
          {/* Header & Controls */}
          <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Event Dashboard</h1>
              <div className="flex items-center gap-2 mt-1">
                <span className={`w-2 h-2 rounded-full ${detailsQueryFetching ? 'bg-amber-400 animate-pulse' : 'bg-emerald-500'}`}></span>
                <p className="text-slate-500 text-sm">
                  {detailsQueryFetching? 'Syncing live data...' : 'System Operational'}
                </p>
              </div>
            </div>
            
            <select 
              value={selectedEventData?.eventId}
              onChange={(e:any) => {
                  const data = eventListQuery.data?.filter(d=>d.eventId === Number(e.target.value));
                  if (data && data.length > 0)
                  {
                    setSelectedEventData(data[0] as EventDetailsDTO);
                    // console.log(`selected event now is ${data[0].eventDate}`);
                    // console.log(`selected event details:`, data[0]);
                  }
              }}
              className="bg-white border border-slate-300 text-slate-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-64 p-2.5 shadow-sm"
            >
              {eventListQuery.data?.map( e=> (
              <>
                <option  key={e.eventId} value={(e.eventId)}>{e.eventName}</option>
              </>
              ))}
            </select>
          </div>

          {/* KPI Grid - Independent Loading States */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            
            {/* A. Financials (Depends on Details Query) */}
            {detailsQueryFetching ? (
              <LoadingSection />
            ) : (
              <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200">
                <div className="flex justify-between items-start">
                  <div>
                    <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Net Revenue</p>
                    <h2 className="text-3xl font-bold text-slate-900 mt-2">
                      ${financialSummary ? Number(financialSummary.net).toLocaleString() : '0'}
                    </h2>
                    <div className="flex gap-2 mt-2 text-xs text-slate-400 font-medium">
                      <span className="bg-emerald-50 text-emerald-700 px-1.5 py-0.5 rounded">Gross: ${financialSummary ? Number(financialSummary.gross).toLocaleString() : '0'}</span>
                    </div>
                  </div>
                  <div className="p-2 bg-slate-50 rounded-lg text-slate-400">
                    <span className="text-xl">$</span>
                  </div>
                </div>
              </div>
            )}

            {/* B. Velocity (Depends on Velocity Query) */}
            {velocityQuery.isLoading ? (
              <LoadingSection />
            ) : (
              <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200">
                <div className="flex justify-between items-center h-full">
                  <div>
                    <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Velocity (24h)</p>
                    <div className="flex items-baseline gap-2 mt-2">
                      <h2 className="text-3xl font-bold text-slate-900">{velocityQuery.data?.velocity24h ?? 0}</h2>
                      <span className="text-xs text-slate-500 font-medium">tickets</span>
                    </div>
                    <p className={`text-xs mt-1 font-bold ${((velocityQuery.data?.velocity24h ?? 0) >= (velocityQuery.data?.velocity7dAvg ?? 0)) ? 'text-emerald-600' : 'text-amber-500'}`}>
                      {((velocityQuery.data?.velocity24h ?? 0) > (velocityQuery.data?.velocity7dAvg ?? 0)) ? '↑ Trending Up' : '↓ Cooling Off'}
                    </p>
                  </div>
                  <div className="h-12 w-24">
                    <Sparkline 
                      data={velocityQuery.data?.trend ?? [0, 0, 0, 0, 0, 0, 0]} 
                      color={((velocityQuery.data?.velocity24h ?? 0) >= (velocityQuery.data?.velocity7dAvg ?? 0)) ? 'stroke-emerald-500' : 'stroke-amber-500'} 
                    />
                  </div>
                </div>
              </div>
            )}

            {/* C. Check-Ins (Depends on CheckIn Query) */}
            {checkInQuery.isLoading ? (
              <LoadingSection />
            ) : (
              <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-200">
                <div className="flex justify-between items-center h-full">
                  <div>
                    <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Real-time Check-ins</p>
                    <div className="flex items-baseline gap-2 mt-2">
                      <h2 className="text-3xl font-bold text-slate-900">{checkInQuery.data?.checkedIn ?? 0}</h2>
                      <span className="text-xs text-slate-500 font-medium">/ {checkInQuery.data?.totalSold ?? 0}</span>
                    </div>
                    {/* <p className="text-xs text-blue-600 mt-1 font-medium cursor-pointer hover:underline">
                      View Guest List →
                    </p> */}
                  </div>
                  <CheckInDonut current={checkInQuery.data?.checkedIn ?? 0} total={checkInQuery.data?.totalSold ?? 0} />
                </div>
              </div>
            )}
          </div>

          {/* Main Data Table (Depends on Details Query) */}
          <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden min-h-[300px]">
            <div className="px-6 py-4 border-b border-slate-100 bg-gray-50 flex justify-between items-center">
              <h3 className="font-semibold text-slate-800">Ticket Types Breakdown</h3>
            </div>
            
            {detailsQueryFetching? (
              <div className="flex flex-col items-center justify-center h-64 space-y-4">
                <div className="w-10 h-10 border-4 border-slate-200 border-t-blue-600 rounded-full animate-spin"></div>
                <p className="text-slate-400 text-sm font-medium">Loading ticket allocations...</p>
              </div>
            ) : (
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-100">
                    <th className="px-6 py-4 font-bold">Ticket Type</th>
                    <th className="px-6 py-4 font-bold">Capacity</th>
                    <th className="px-6 py-4 font-bold text-right">Sold</th>
                    <th className="px-6 py-4 font-bold text-right">Revenue</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-50">
                  {detailsQueryData?.map((tier) => {
                    console.log(`tier is ${tier}`);
                    const percent = Math.min(100, Math.round((tier.ticketsSold / tier.totalAllowed) * 100));
                    
                    return (
                      <tr key={tier.eventItemTypeId} className="hover:bg-slate-50 transition-colors group">
                        <td className="px-6 py-4">
                          <div className="font-medium text-slate-900">{tier.name}</div>
                          <div className="text-xs text-slate-400">${tier.cost.toFixed(2)}</div>
                        </td>
                        
                        {/* Progress Bar Column */}
                        <td className="px-6 py-4 w-1/3">
                          <div className="flex flex-col gap-1">
                            <div className="flex justify-between text-xs mb-1">
                              <span className="font-medium text-slate-600">{percent}% Full</span>
                            </div>
                            <div className="w-full h-2 bg-slate-100 rounded-full overflow-hidden">
                              <div 
                                className={`h-full rounded-full transition-all duration-1000 ${percent >= 90 ? 'bg-amber-500' : 'bg-blue-600'}`} 
                                style={{ width: `${percent}%` }}
                              />
                            </div>
                          </div>
                        </td>
                        
                        <td className="px-6 py-4 text-right text-sm text-slate-600">
                          <span className="font-bold text-slate-900">{tier.ticketsSold}</span> 
                          <span className="text-slate-400"> / {tier.totalAllowed}</span>
                        </td>
                        
                        <td className="px-6 py-4 text-right font-medium text-slate-900 font-mono">
                          ${(tier.ticketsSold * tier.cost).toLocaleString()}
                        </td>
                      </tr> 
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
        
    </div>
       <Footer/>
    </IonContent>
    </IonPage>
  );
}
