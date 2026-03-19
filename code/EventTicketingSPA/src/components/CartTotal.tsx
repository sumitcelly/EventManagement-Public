import { useWatch,Control } from "react-hook-form";
import { TicketFormValues  ,Ticket } from "../types/Tickets";
import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { number } from "yup";
import { useEffect, useState } from "react";

type CartTotalProps = {
  control: Control<TicketFormValues>;
};

  /**
 * Calculates the total to show the customer so the organizer gets a clean net amount.
 * @param {number} targetNet - Amount in cents organizer should keep (e.g., 2000 for $20)
 * @param {number} platformProfit - Your profit in cents (e.g., 100 for $1)
 * @param {number} stripePercent - Stripe % fee (default 0.029 for 2.9%)
 * @param {number} stripeFixed - Stripe fixed fee in cents (default 30)
 */


const letCustomeAbsorbAllFees = false;

export const calculateForCustomerAbsorbsAllFees = (
    targetNet: number, 
    platformFees: number, 
    stripePercent: number = 0.029, 
    stripeFixed: number = 30
  ): { totalToCharge: number; serviceFee: number; displayTotal: string; displayFee: string , stripeFees:string} => {
    // Formula: Total = (Net + Fixed + Profit) / (1 - Percent)

    if (targetNet <=0) {
      return {
        totalToCharge: 0, 
        serviceFee: 0,
        displayTotal: "0.00",
        displayFee: "0.00",
        stripeFees: "0.00"
      };
    }   

    const platformFeeAmt = parseFloat((targetNet * platformFees).toFixed(2));
    const numerator = targetNet*100 + stripeFixed + platformFeeAmt*100;
    const denominator = 1 - stripePercent;
    
    const totalToCharge = Math.ceil(numerator / denominator);
    
    // Total fees shown to the customer (Your profit + the stripe fee you are passing on)
    const serviceFee = totalToCharge - targetNet*100;

    return {
      totalToCharge, // The final price (e.g., 2195)
      serviceFee,    // The "Booking Fee" line item (e.g., 195)
      displayTotal: (totalToCharge / 100).toFixed(2),
      displayFee: (serviceFee / 100).toFixed(2),
      stripeFees: String(totalToCharge-targetNet-platformFeeAmt)
    };
  };

  export const calculateForOrganizerAbsorbsStripeFees =
    (total: number, platformFees:number,stripePercent: number = 0.029, 
    stripeFixed: number = 30 )=>
  {
     const platformFeeAmt = parseFloat((total * platformFees).toFixed(2));
     const displayTotal= (total + platformFeeAmt).toFixed(2);
     const displayFee= platformFeeAmt.toFixed(2);
     console.log('fees',stripeFixed,displayTotal,(Number(displayTotal)*stripePercent*100).toFixed(2), stripeFixed);
     const stripeFees = (((total + platformFeeAmt)*stripePercent*100)+ stripeFixed).toFixed(2);
     return {displayTotal, displayFee, stripeFees};

  }
  
  
  export default function CartTotal({ control }: CartTotalProps) {
    const tickets = useWatch({ control, name: "tickets" });
    const [fees, setFees]=useState("0");
    const [total, setTotal]=useState("0");

    const { data, isLoading } = 
    useQuery(['TransactionFees'], async () => {
      const res = await axiosClient.get(`/payment/transactionfees`);
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

  useEffect(()=>{
      console.log('tickets is',tickets);

      console.log('data is',data);
      const total = tickets.reduce((sum: number, t:Ticket) => sum + (t.quantity || 0) * t.cost, 0);
      console.log('cart total',total);

      const platformFees = parseFloat((total * Number(data?.platformFees)).toFixed(2));

      console.log('total and plattform fees',total,platformFees);

      let displayTotal="", displayFee="";
      if (letCustomeAbsorbAllFees){
        ({displayTotal, displayFee} = calculateForCustomerAbsorbsAllFees(total,  data?.platformFees, Number(data?.stripeFees), Number(data?.stripeFixed)));
        setTotal(displayTotal);
        setFees(displayFee);
      
      }
      else
      {
        ({displayTotal, displayFee} = calculateForOrganizerAbsorbsStripeFees(total, data?.platformFees));
        setTotal(displayTotal);
        setFees(displayFee);
      
      }

      //const finalTotal = (total + processingFees + platformFees).toFixed(2);
      console.log(displayTotal);
      console.log(displayFee);``

 
  },[data,tickets]);
   return (<>
               <div className="grid grid-cols-2 gap-y-1">
                <div className="text-left">Service Fee:</div>
                <div className="text-right">${fees}</div>

                <div className="text-left font-semibold">Total:</div>
                <div className="text-right font-semibold">${total}</div>
              </div>
          </>
  );
  
}
