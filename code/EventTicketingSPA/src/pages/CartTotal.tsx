import { useWatch,Control } from "react-hook-form";
import { TicketFormValues  ,Ticket } from "../types/Tickets";

type CartTotalProps = {
  control: Control<TicketFormValues>;
};


export default function CartTotal({ control }: CartTotalProps) {
  const tickets = useWatch({ control, name: "tickets" });
  
  const total = tickets.reduce((sum: number, t:Ticket) => sum + (t.quantity || 0) * t.cost, 0);
  const processingFees = parseFloat((total *.03).toFixed(2));
  const platformFees =1;
  const finalTotal = total + processingFees + platformFees;
  console.log(processingFees);
  console.log(finalTotal);
  return (<>
            
              {tickets?.filter(list =>list.quantity>0).map((item) =>
                (
                    <div key={item.id} className = "flex flex-row">
                      <div className="text-l text-secondary-color w-1/2 text-left w-3/4">{item.name}</div>
                      <div className="text-l text-secondary-color w-1/4">{item.quantity}</div>
                      <div className="text-l text-secondary-color w-1/4">${item.quantity*item.cost}</div>
                    </div>
                ))
                } 

              <div className="mt-auto">
                <div className="text-s text-bold text-primary-color">Stripe fees: <span className="mr-auto">${processingFees}</span></div>
                <div className="text-s text-bold text-primary-color">Platform fees: ${platformFees}</div>
                <div className="text-l text-bold text-primary-color">Total: ${finalTotal}</div>
              </div>
          </>
  );
  
}
