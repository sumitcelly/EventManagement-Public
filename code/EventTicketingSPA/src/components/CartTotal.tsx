import { useWatch,Control } from "react-hook-form";
import { TicketFormValues  ,Ticket } from "../types/Tickets";

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
const calculateGrossUp = (
    targetNet: number, 
    platformProfit: number, 
    stripePercent: number = 0.029, 
    stripeFixed: number = 30
  ): { totalToCharge: number; serviceFee: number; displayTotal: string; displayFee: string } => {
    // Formula: Total = (Net + Fixed + Profit) / (1 - Percent)

    if (targetNet <=0) {
      return {
        totalToCharge: 0, 
        serviceFee: 0,
        displayTotal: "0.00",
        displayFee: "0.00"
      };
    }   
    const numerator = targetNet + stripeFixed + platformProfit;
    const denominator = 1 - stripePercent;
    
    const totalToCharge = Math.ceil(numerator / denominator);
    
    // Total fees shown to the customer (Your profit + the stripe fee you are passing on)
    const serviceFee = totalToCharge - targetNet;

    return {
      totalToCharge, // The final price (e.g., 2195)
      serviceFee,    // The "Booking Fee" line item (e.g., 195)
      displayTotal: (totalToCharge / 100).toFixed(2),
      displayFee: (serviceFee / 100).toFixed(2)
    };
  };

export default function CartTotal({ control }: CartTotalProps) {
  const tickets = useWatch({ control, name: "tickets" });
  console.log('tickets is',tickets);
  const total = tickets.reduce((sum: number, t:Ticket) => sum + (t.quantity || 0) * t.cost, 0);
  console.log('cart total',total);
  //const processingFees = parseFloat((total *.029).toFixed(2));
  const platformFees = parseFloat((total *.03).toFixed(2));

  console.log('total and plattform fees',total,platformFees);

  let displayTotal="", displayFee="";
  if (letCustomeAbsorbAllFees){
    ({displayTotal, displayFee} = calculateGrossUp(total * 100, platformFees * 100));
  }
  else
  {
    displayTotal= (total + platformFees).toFixed(2);
    displayFee= platformFees.toFixed(2);
  }

  //const finalTotal = (total + processingFees + platformFees).toFixed(2);
  console.log(displayTotal);
  console.log(displayFee);

  return (<>
            
     
              <div className="grid grid-cols-2 gap-y-1">
                <div className="text-left">Service Fee:</div>
                <div className="text-right">${displayFee}</div>

                <div className="text-left font-semibold">Total:</div>
                <div className="text-right font-semibold">${displayTotal}</div>
              </div>
          </>
  );
  
}
