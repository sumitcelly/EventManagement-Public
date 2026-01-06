import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";

import { useMutation } from "react-query";
import toast, { Toaster } from 'react-hot-toast';
import { OrganizerInfo } from "../../types/Organizer";
import { useEffect, useState } from "react";



  
export default function OrganizerStripe({organizerInfo, organizerId}: {organizerInfo?: OrganizerInfo, organizerId?:string}) {
  
const [stripeAcctId, setStripeAcctId] = useState(organizerInfo?.stripeAccountId);
const [stripeStatus,setStripeStatus] = useState(organizerInfo?.stripeConnectStatus);

    console.log(`stripe acctid ${stripeAcctId} and status is ${stripeStatus}`);

    const { data:liveStripeStatus, isLoading } = useQuery(['validateStripeStatus',organizerId], async () => {
    const res = await axiosClient.get(`/payment/connect-status/${stripeAcctId}`);
    console.log('stripe validation details from backend', res?.data);
    return res.data;
    },
    {
    staleTime: 1000 * 60 * 5,
    enabled: !!stripeAcctId
    }
    );

  useEffect(() => {
      console.log('MemberInfo changed:', organizerInfo);
      if (organizerInfo) {
        setStripeAcctId(organizerInfo.stripeAccountId);
        setStripeStatus(organizerInfo.stripeConnectStatus);   
      }
    }, [organizerInfo]);
    

  const createStripeAccount = async()=>{
    try
    {
        const res = await axiosClient.post(`/payment/create-account`,organizerId, { headers: {
        'Content-Type': 'application/json'}
    });
        if (res.status == 200 && res.data)
        {
            linkStripeAccount(res.data);
        }
        else
        {
             toast.error(`Error when creating stripe account ${res.status}`);
             return null;
        }
    }
    catch(error)
    {
        toast.error(`Error when creating stripe account for organizer. ${error}`);
        console.log(`Error when creating stripe account for organizer. ${error}`);
    }
  }

  const linkStripeAccount = async(stripeId:string | undefined) =>{
    try
    {
        const res = await axiosClient.post(`/payment/initiate-account-link/${organizerId}`,stripeId,
        {
             headers: {
            'Content-Type': 'application/json'}
        });
        if (res.status == 200)
            window.location.href = res?.data;
        else
            toast.error(`Error when linking stripe account ${res.status}`);
    }
    catch(error)
    {
        toast.error(`Error when linking stripe account . ${error}`);
        console.log(`Error when linking stripe account . ${error}`);
    }
  }

  

  if (isLoading) return <p>Loading...</p>;
  
  return (  
     
    <div className="max-w-md mx-auto  text-center">
      {/* <h2 className="text-2xl font-semibold mb-4 text-accent-color font-accent">Go Live!</h2> */}
      <div className="flex flex-col items-center">
         <Toaster position="top-right" />
         {/**We have nothing with stripe*/}
         {(!stripeAcctId || !stripeStatus) &&(
            <div className="flex items-center">
                <button 
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                onClick={()=>createStripeAccount()}
                >
                    Connect to Stripe
                </button>
            </div>
         )}
         
         {/*We have stripe id but live status is false */}
         {(stripeAcctId && !liveStripeStatus)  &&(
            <>
            <label className="font-semibold text-secondary-color items-center">Your Stripe connection for stripe account <i>{stripeAcctId}</i> is not complete.</label>
            {stripeStatus === "RequirementsPending" &&(
                 <label className="font-semibold text-secondary-color mt-2">There are requirments pending on your stripe account.</label>
            )}

            <div 
            className="bg-brand-dark text-white text-xs mt-2  text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
            onClick={()=>linkStripeAccount(stripeAcctId)}
            >
                Complete Stripe Connection
            </div>
            </>
         )}


         {liveStripeStatus &&(
            <>
            <label className="font-semibold text-go-color mt-2">You are all connected to Stripe!</label>
            <label className="font-semibold text-go-color mt-2">Stripe Acct Id: {stripeAcctId}</label>
            </>
         )}
         
      </div>

  </div>
 
  );
}
