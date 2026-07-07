import { useQuery, useQueryClient } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";

import { useMutation } from "react-query";
import toast, { Toaster } from 'react-hot-toast';
import { OrganizerInfo } from "../../types/Organizer";
import { useEffect, useState } from "react";



  
export default function OrganizerStripe({organizerInfo, organizerId}: {organizerInfo?: OrganizerInfo, organizerId?:string}) {

const stripeAcctId = organizerInfo?.stripeAccountId;
//const [stripeTaxStatus,setStripeTaxStatus] = useState("");

    console.log(`stripe acctid ${stripeAcctId} and organizerId ${organizerId}`);

    const { data:liveStripeStatus, isLoading } = useQuery(['validateStripeStatus',organizerId], 
    async () => {
        const res = await axiosClient.get(`/payment/connect-status/${organizerId}/${stripeAcctId}`);
        console.log('stripe validation details from backend', res?.data);
        return res.data;
    },
    {
        staleTime: 1000 * 60 * 5,
        enabled: !!stripeAcctId
    }
    );

    const { data:liveStripeTaxStatus, isLoading:isTaxLoading } =
    useQuery(['gettaxStatus',organizerId], async () => {
        const res = await axiosClient.get(`/payment/gettaxstatus/${stripeAcctId}`);
        console.log('stripe tax status from backend', res?.data);
        
        return res.data;
    },
    {
    staleTime: 1000 * 60 * 5,
    enabled: !!stripeAcctId
    }
    );
    

  const createStripeAccount = async()=>{
    try
    {
        toast.loading("Redirecting to stripe for account creation...");
        const res = await axiosClient.post(`/payment/create-account/${organizerId}`, { headers: {
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
        toast.loading("Redirecting to stripe for account linking...");
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
     
    <div className="max-w-md mx-auto">
      {/* <h2 className="text-2xl font-semibold mb-4 text-accent-color font-accent">Go Live!</h2> */}
      <div className="flex flex-col items-center">
         {/* <Toaster position="top-right" /> */}
         {/**We have nothing with stripe*/}
         {(!stripeAcctId || !liveStripeStatus.connected) &&(
            <div className="flex items-center">
                <button 
                className="ml-auto bg-brand-dark text-white text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                onClick={()=>createStripeAccount()}
                >
                    Connect to Stripe
                </button>
            </div>
         )}
         
         {/*We have stripe id. Lets check if something is pending */}
         {(stripeAcctId && liveStripeStatus.connected)  && (
            <>
            <div className="font-semibold text-secondary-color">Stripe Integration Checklist for <i>{stripeAcctId}</i></div>
            
            <div className="flex flex-col mt-2">
                <div className="font-semibold text-secondary-color">
                    <span>{!liveStripeStatus.requirementsPending ? "✅" : "❌"} Requirements Collected</span>
                </div>

                <div className="font-semibold text-secondary-color mt-2">
                    <span>{liveStripeStatus.chargesEnabled ? "✅" : "❌"} Charges Enabled</span>
                    {!liveStripeStatus.chargesEnabled && <p className="error">Stripe is still verifying your business details.</p>}
                </div>

                <div className="font-semibold text-secondary-color mt-2">
                    <span>{liveStripeStatus.payoutsEnabled ? "✅" : "❌"} Bank Account Verified for Payouts</span>
                </div>
            </div>
            
           {!liveStripeStatus.chargesEnabled || 
            !liveStripeStatus.payoutsEnabled || 
            !liveStripeStatus.detailsSubmitted ||
            liveStripeStatus.requirementsPending
            ? (
                <button 
                    className="bg-brand-dark text-white text-xs mt-2  text-brand-neutral px-2 py-2 rounded hover:bg-blue-700"
                    onClick={()=>linkStripeAccount(stripeAcctId)}
                    >
                    Complete Stripe Connection
                </button>
             )
            : 
            (
                 <div className="flex flex-col mt-2">
                    <div className="font-semibold text-go-color mt-2">You are all connected to Stripe!</div>
                   
                    {/* <div className="font-semibold text-go-color mt-2">Stripe Acct Id: {stripeAcctId}</div> */}
                
                </div>
            )
            
        }

         {liveStripeTaxStatus && liveStripeTaxStatus == "active" &&(
          
            <div className="font-semibold text-secondary-color mt-2">
                    <span>{"✅"} Tax Status: Active</span>
            </div>
           
           
         )}

          {liveStripeTaxStatus && liveStripeTaxStatus == "pending" &&(
            <>
             <div className="font-semibold text-secondary-color mt-2">
                    <span>{"❌"} Tax Status: Pending</span>
            </div>
            <div className="font-semibold text-secondary-color mt-2">Please update your tax information by 
                visiting  https://dashboard.stripe.com/tax/setup.</div>
            </>
         )}
         

        </>
         )}
      </div>
    </div>
 
  )
}
