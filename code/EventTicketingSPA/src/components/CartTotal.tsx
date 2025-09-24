import { useWatch,Control } from "react-hook-form";
import { TicketFormValues  ,Ticket } from "../types/Tickets";

type CartTotalProps = {
  control: Control<TicketFormValues>;
};


export default function CartTotal({ control }: CartTotalProps) {
  const tickets = useWatch({ control, name: "tickets" });
  console.log('tickets is',tickets);
  const total = tickets.reduce((sum: number, t:Ticket) => sum + (t.quantity || 0) * t.cost, 0);
  console.log('cart total',total);
  const processingFees = parseFloat((total *.03).toFixed(2));
  const platformFees =1;
  const finalTotal = total + processingFees + platformFees;
  console.log(processingFees);
  console.log(finalTotal);
  return (<>
            
              {/* {tickets?.filter(list =>list.quantity>0).map((item) =>
                (
                    <div key={item.id} className = "flex flex-row">
                      <div className="text-l text-secondary-color w-1/2 text-left w-3/4">{item.name}</div>
                      <div className="text-l text-secondary-color w-1/4">{item.quantity}</div>
                      <div className="text-l text-secondary-color w-1/4">${item.quantity*item.cost}</div>
                    </div>
                ))
                }  */}
     
              <div className="grid grid-cols-2 gap-y-1">
                <div className="text-left">Stripe fees:</div>
                <div className="text-right">${processingFees}</div>

                <div className="text-left">Platform fees:</div>
                <div className="text-right">${platformFees}</div>

                <div className="text-left font-semibold">Total:</div>
                <div className="text-right font-semibold">${finalTotal}</div>
              </div>
          </>
  );
  
}
