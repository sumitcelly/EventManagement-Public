import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { EventHeader } from "../../types/Event";
import  ListMenu  from "../../components/ListMenu";
import { ListMenuData } from "../../components/ListMenu";
import { useEffect, useRef, useState } from "react";
import { useMutation } from "react-query";
import toast, { Toaster } from 'react-hot-toast';
import { IonPage, IonHeader, IonContent } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import { calculateForCustomerAbsorbsAllFees, calculateForOrganizerAbsorbsStripeFees } from "../../components/CartTotal";
import { updateCustomerProfile } from "../../features/auth/authSlice";
import { useDispatch } from "react-redux";

  
export default function EventPublish({eventId,isActive}: {eventId?:string,isActive:boolean}) {
  const history = useHistory();
  const dispatch = useDispatch();
  
  const [fees, setFees]=useState("0");
  const [total, setTotal]=useState("0");
  const [stripeFees, setStripeFees]=useState("0");

  const [feeMode, setFeeMode]=useState("0");
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);

  const queryClient = useQueryClient();
  const refundModeRef = useRef<HTMLSelectElement>(null);
  const isLiveRef = useRef<HTMLInputElement>(null);
  const user = useAppSelector((state: RootState) => state?.auth.user);
  console.log('customer url name', user?.customerUrlName);
  const eventData = useAppSelector((state: RootState) => state?.event);
  console.log ('event id in publish page', eventId);

  const { data, isLoading:validateLoading } = useQuery(['settings',eventId], async () => {
    const res = await axiosClient.get(`/events/settings/${eventId}`);
    console.log('Event settings details from backend', res?.data);
    return res.data;
  },
    {
      staleTime: 1000 * 60 * 5,
      enabled: !!eventId && isActive
    }
  );

   const { data:transactionFees, isLoading:transLoading } = 
    useQuery(['TransactionFees', eventId], async () => {
      const res = await axiosClient.get(`/payment/transactionfees/${eventId}`);
      console.log('TransactionFees fetched from backend',res.data);
      return res.data;
  
    },
    {
     staleTime: 1000 * 60 * 600,  // Data stays fresh for 5 minutes
     cacheTime: 1000 * 60 * 600, // Cache persists for 30 minutes
     refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,   
    }
  );

     const { data:customerData, isLoading:isLoadingCustomer } = 
      useQuery(['OrganizerInfo',user?.customerId], async () => {
          console.log("Fetching organizer by customer id", user?.customerId);
          const res = await axiosClient.get(`/EventOrganizer/${user?.customerId}`);
          console.log('organizer Indo',res?.data, res?.status);
    
          return res?.data;
        },
        {
          staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
          cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
          enabled: !!user?.customerId && !user?.customerUrlName //  only run query if we have an id
        }
      );
    
  useEffect(()=>{
    if (customerData && customerData?.organizerEventBaseUrl)
      dispatch(updateCustomerProfile({customerUrlName:customerData?.organizerEventBaseUrl,stripeConnectStatus: customerData?.stripeConnectStatus}));
  },[customerData]);


  type PublishEventParams = {
    status: boolean;
    eventId?: string;
    refundMode?:string;
    ticketFeeMode?: string;
    eventUrlName?:string;
    organizerUrlName?: string;
  };

  
  const publishEvent = async ({ status, eventId ,refundMode, ticketFeeMode, organizerUrlName, eventUrlName}: PublishEventParams) => {
    try {
      console.log("Publishing event", eventId, "set live status to", status);

      const res = await axiosClient.put(`/events/eventsettings/${eventId}`, {
        organizerUrlName: organizerUrlName,
        eventUrlName:eventUrlName,
        isLive: status,
        eventDate: eventData?.eventDate,
        refundMode: Number(refundMode) || 0,
        ticketFeeMode: Number(ticketFeeMode) || 0
      }, {
                  headers: {
                  'Content-Type': 'application/json'}
                });
            
      console.log("Event publish API response:", res?.data);
      const eventStatus = status?"Live":"Draft";

      if (res?.data) {
        await queryClient.resetQueries({ queryKey: ["settings", eventId] });
        console.log("✅ Success publishing event", eventId);
        toast.success("Event status changed successfully to "+eventStatus);
        return true;
      } else {
        console.warn("⚠️ There was an issue publishing your event", eventId);
         toast.error("Event failed to change event status. Please try again!");
        return false;
      }
    } catch (error) {
      console.error("❌ Error publishing event", error);
      return false;
    }
  };

  const handleModeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newValue = e.target.value;
    setFeeMode(newValue); // This triggers the re-render automatically
  };

  const mutation = useMutation<boolean, Error, PublishEventParams>({
    mutationFn: publishEvent,
  });

  const { mutate, isLoading, isSuccess, isError } = mutation;

  useEffect(()=>{
    if (!transactionFees)
      return;

    let displayTotal ="0";
    let displayFee ="0"
    let stripeFees ="0";
    if (feeMode =="1")
    {
    
      ({displayTotal, displayFee, stripeFees} = calculateForCustomerAbsorbsAllFees(20,  
                                    transactionFees?.platformFees,
                                    Number(transactionFees?.stripeFees),
                                    Number(transactionFees?.stripeFixed),
                                    Number(transactionFees?.floor),1));
       
    }
    if (feeMode == "2")
    {

       ({displayTotal, displayFee, stripeFees} = calculateForOrganizerAbsorbsStripeFees(20, 
                            transactionFees?.platformFees,
                            Number(transactionFees?.stripeFees),
                            Number(transactionFees?.stripeFixed),
                            Number(transactionFees?.floor),1));
        
        console.log('org absorbs',displayTotal,displayFee, stripeFees);
    }
     setTotal(displayTotal);
     setFees(displayFee);
     setStripeFees(stripeFees);

  },[feeMode,transactionFees]);

  useEffect(()=>{
    if (data && refundModeRef.current && isLiveRef.current)
    {
      refundModeRef.current.value = data.refundMode;
      //ticketDisplayModeRef.current.value = data.ticketFeeMode;
      isLiveRef.current.checked = data.isLive;
      
    }
    setFeeMode(data?.ticketFeeMode);
  },[data]);

  
  if (validateLoading || transLoading) return <p>Loading...</p>;
 
  return (  
     
    <div className="max-w-md mx-auto  text-center">
      {/* <h2 className="text-2xl font-semibold mb-4 text-accent-color font-accent">Go Live!</h2> */}
      <div className="flex flex-col">
         {/* <Toaster position="top-right" /> */}
         <label className="block font-semibold italic font-accent text-accent-color">Your event is in {data?.isLive?'Live':'Draft'} status</label>
         {data && !data.isLive?(
          <div className="bg-brand-neutral mt-6 p-3 text-center text-secondary-color rounded">
       
            {
              data && data.ticketStatus && (
              <p className="text-l">
                You are ready to go online! All event and ticket details are complete!
              </p>
            )}
             {
              data && !data.ticketStatus && (
              <p className=" text-l">
               Please add atleast one ticket type to go live.
              </p>
            )}
          </div>
          ):
            <div className="bg-brand-neutral mt-6 rounded p-2 text-center">
              <p className="text-secondary-color text-l text-center">
                  You are already online. Any changes you make to your events will be available instantly!
                </p>
            </div>
        }


        {data  && (
          // <div className= "p-2 mt-3 bg-brand-neutral rounded min-h-[40px]">
             <div
              onClick={(e) => {
                e.stopPropagation();
                history.push(`/eventdetails/${user?.customerUrlName}/${data.eventUrlName}`,{mode: "preview"});
              }}
              title={`${window.location.origin}/eventdetails/${user?.customerUrlName}/${data.eventUrlName}`}
              className="p-2 mt-3 bg-brand-neutral rounded min-h-[40px] cursor-pointer truncate"
            >
              Your event url is {`${window.location.origin}/eventdetails/${user?.customerUrlName}/${data.eventUrlName}`}
            </div>
         
        )}
        
        <div className="flex flex-col space-y-2 mt-6">
          <div className="flex flex-row space-x-2  items-center mb-2">
            <label htmlFor="isLive">Is Live</label>
            <input
            type="checkbox"
            id="isLive"
            ref={isLiveRef}
            disabled={!data || (data && !data.ticketStatus)}
            defaultChecked={data && data.isLive?true: false}/>
          </div>
        

         <label className="font-semibold mr-auto">Refund Mode</label>
           <select ref={refundModeRef} className="w-3/5 rounded-md border border-gray-300 bg-white py-2 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="0">No refunds allowed</option>
              <option value="1">Customer initiates refunds</option>
            </select>
        </div>

         <div className="flex flex-col space-y-1 mt-4">
          <label className="block font-semibold mb-1 mr-auto">Fee Mode</label>
           <select value={feeMode} className="w-3/5 rounded-md border border-gray-300 bg-white py-2 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" onChange={handleModeChange} >
              <option value="0">None(None required for free tickets)</option>
              <option value="1">Customer absorbs Stripe fees</option>
              <option value="2">Organizer absorbs Stripe fees</option>
            </select>

            {feeMode !=="0" && (
             <div className="mt-4 rounded-lg border border-gray-200 bg-gray-50 p-4 shadow-sm dark:border-gray-700 dark:bg-gray-800">
                {/* Header */}
                <div className="mb-3 border-b border-gray-200 pb-2 text-sm font-semibold text-gray-700 dark:border-gray-700 dark:text-gray-300">
                  Sample Breakdown for a $20.00 Ticket
                </div>

                {/* Data Grid */}
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-gray-500">Ticket Price:</span>
                    <span className="font-medium text-gray-900 dark:text-white">$20.00</span>
                  </div>

                  <div className="flex justify-between">
                    <span className="text-gray-500">Service Fees:</span>
                    <span className="font-medium text-gray-900 dark:text-white">{fees}</span>
                  </div>

                  <div className="flex justify-between border-t border-gray-200 pt-2 dark:border-gray-700">
                    <span className="font-bold text-gray-900 dark:text-white">Customer Pays:</span>
                    <span className="font-bold text-blue-600 dark:text-blue-400">{total}</span>
                  </div>

                  <div className="mt-2 flex justify-between rounded-md bg-green-50 p-2 dark:bg-green-900/20">
                    <span className="font-semibold text-green-700 dark:text-green-400">Your Payout:</span>
                    <span className="font-bold text-green-800 dark:text-green-300">
                      {/* Using Number() to handle the .NET integer/string mismatch */}
                      {Number(feeMode) === 1 ? '$20.00' : '$'+((2000 - Number(stripeFees))/100).toFixed(2)}
                    </span>
                  </div>
                </div>
                </div>

            )}
        </div>

        {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm transition-opacity">
          
          {/* Modal Container Card */}
          <div className="relative bg-white max-w-lg w-full rounded-2xl p-6 shadow-2xl border border-slate-100 flex flex-col space-y-4 animate-in fade-in zoom-in-95 duration-200">
            
            {/* Header */}
            <div className="flex items-center space-x-3">
              <span className="text-2xl">🧪</span>
              <h3 className="text-lg font-bold text-slate-900">Run a Checkout Simulation</h3>
            </div>

            {/* Description Paragraphs */}
            <div className="text-slate-600 text-sm space-y-3 leading-relaxed">
              <p>
                Before publishing your event live to the public, let's verify that your 
                ticket delivery system, email configurations, and Stripe account connections 
                are functioning perfectly.
              </p>
              <p>
                Clicking below will launch a secure sandbox environment. You will be redirected 
                to a test payment page where you can complete a mock checkout using a fake credit card.
              </p>
            </div>

            {/* Feature Highlights Grid */}
            <div className="bg-slate-50 rounded-xl p-4 text-xs text-slate-700 space-y-2 border border-slate-100">
              <div className="flex items-start space-x-2">
                <span className="text-indigo-600 font-bold">👉</span>
                <p><strong>What to expect:</strong> You will receive a fully functional test ticket in your email inbox complete with its scanning barcode.</p>
              </div>
              <div className="flex items-start space-x-2">
                <span className="text-indigo-600 font-bold">👉</span>
                <p><strong>Cost:</strong> Free ($0.00). No real money will be processed or charged.</p>
              </div>
            </div>

            {/* Action Buttons Container */}
            <div className="flex flex-col sm:flex-row-reverse sm:space-x-reverse sm:space-x-3 pt-2">
              <button 
                onClick={() => {history.push(`/buytickets`,{id: eventId, simulationMode: true, organizerId: user?.customerId}); setIsModalOpen(false);}} 
              
                className="w-full sm:w-auto inline-flex justify-center rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:bg-indigo-400 disabled:cursor-not-allowed transition-all"
              >
                Run Simulation
              </button>
              
              <button 
                onClick={() => setIsModalOpen(false)} 
                // disabled={isLoading}
                className="w-full sm:w-auto mt-2 sm:mt-0 inline-flex justify-center rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm ring-1 ring-inset ring-slate-200 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
              >
                Cancel and Dismiss
              </button>
            </div>

          </div>
        </div>
      )}

      </div>
    
    {data && (
      <div className="flex flex-row mt-4">
          <button
                className="bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                onClick={() => setIsModalOpen(true)}
                disabled={mutation.isLoading}
              >
                Run Checkout Simulation
          </button>
          <div className="flex-grow"></div>
          <button
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
               onClick={() => mutate({ status: isLiveRef.current?.checked || false, eventId: eventId, 
                      refundMode:refundModeRef.current?.value,
                      ticketFeeMode: feeMode, organizerUrlName: user?.customerUrlName, eventUrlName: data?.eventUrlName })}
                disabled={mutation.isLoading}
              >
               Update
          </button> 
          
         
      </div>
    )}

  </div>
 
  )
}
