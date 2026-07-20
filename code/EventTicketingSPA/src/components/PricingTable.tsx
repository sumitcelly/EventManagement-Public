import { useQuery } from 'react-query';
import axiosClient from '../api/axiosClient';



interface PricingTableProps {
  eventId?: number; // Optional lookup parameter for custom organizer overrides
}

export function PricingTable({ eventId }: PricingTableProps) {
   const {
        data: fees, // provide default empty array
        isLoading,
        error
  } = 
  useQuery(
    ['pricing', 'global'], // structured query key
    async () => {
     
      const res = await axiosClient.get(`/payment/transactionfees/${Number(eventId) || 0}`); // Use eventId if provided, else default to 0 for global
      console.log('Event ticket type details', res?.data);
      return res.data;
    },
     {
      staleTime: 1000 * 60 * 60,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 60, // Cache persists for 30 minutes
      //refetchOnMount: 'always',
      refetchOnWindowFocus: false,
    }
  );

  // Loading Skeleton State
  if (isLoading) {
    return (
       
      <div className="w-full max-w-xl mx-auto p-6 bg-white rounded-2xl border border-slate-100 shadow-md animate-pulse space-y-4">
        <div className="h-6 bg-slate-200 rounded w-1/3"></div>
        <div className="space-y-2 pt-2">
          <div className="h-10 bg-slate-100 rounded"></div>
          <div className="h-10 bg-slate-100 rounded"></div>
          <div className="h-10 bg-slate-100 rounded"></div>
        </div>
      </div>
    );
  }

  // Graceful Error Fallback
  if (error || !fees) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-red-50 text-red-700 text-xs rounded-xl border border-red-100">
        ⚠️ Failed to synchronize live pricing structures: {'Server issues'}. Standard platform base rates apply.
      </div>
    );
  }

  // Helper formatting for currency calculations
  const formatCents = (cents: number) => (cents / 100).toLocaleString('en-US', { style: 'currency', currency: 'USD' });

  return (
    <div className="w-full max-w-xl mx-auto bg-white rounded-2xl border border-slate-100 shadow-xl overflow-hidden animate-in fade-in duration-300">
      
      {/* Header Context Banner */}
      <div className="bg-slate-950 p-5 text-white flex items-center justify-between">
        <div>
          <h3 className="text-base font-bold tracking-tight">Transparent Ticket Pricing</h3>
          <p className="text-slate-400 text-xs">Low-overhead software built for independent organizers.</p>
        </div>
        <span className="text-xl">💳</span>
      </div>

      {/* Pricing Data Table Grid */}
      <div className="p-5 space-y-4">
        <div className="border border-slate-100 rounded-xl overflow-hidden">
          <table className="w-full text-sm text-left border-collapse">
            <thead>
              <tr className="bg-slate-50 text-slate-500 font-semibold text-xs border-b border-slate-100">
                <th className="p-3">Fee Category</th>
                <th className="p-3 text-right">Rate / Charge</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              <tr className="hover:bg-slate-50/50 transition-colors bg-indigo-50/20">
                  <td className="p-3 font-medium text-slate-900">
                    Platform Service Fee
                    <span className="block text-xs font-normal text-slate-500 mt-0.5">
                      {fees.platformFees*100}% of the total order. If the order total is very low, it clamps to a minimum floor of just ${(fees.floor/100).toFixed(2)} total.
                    </span>
                  </td>
                  <td className="p-3 text-right font-mono text-xs font-semibold text-indigo-600 leading-tight">
                    <span className="text-base font-bold">{fees.platformFees*100}%</span>
                    <span className="block font-sans text-[10px] text-slate-400 font-normal mt-0.5">(${(fees.floor/100).toFixed(2)} order min)</span>
                  </td>
               </tr>

              {/* Row 3: Card Processor Cut */}
              <tr className="hover:bg-slate-50/50 transition-colors">
                <td className="p-3 font-medium text-slate-900">
                  Stripe Merchant Fee
                  <span className="block text-xs font-normal text-slate-400 mt-0.5">Standard card rate charged directly by credit payment networks</span>
                </td>
                <td className="p-3 text-right font-mono font-semibold text-slate-900">
                  {(fees.stripeFees*100).toFixed(2)}% + {formatCents(fees.stripeFixed)}
                </td>
              </tr>

            </tbody>
          </table>
        </div>

        {/* Informational Subtext Footer */}
        <div className="bg-slate-50 border border-slate-100 rounded-xl p-3.5 text-xs text-slate-600 leading-relaxed space-y-2">
            <p>
                💡 <strong>How it works:</strong> All transactions route natively through your linked <strong className="text-slate-900 font-semibold">Stripe Connect Standard</strong> node. Ticket sales revenue flows straight to your bank, minus platform components. 
            </p>
            
            {/* Highlighted Bundle Advantage */}
            <div className="pt-1.5 border-t border-slate-200/60 text-indigo-950/90">
                🚀 <strong>The Bundle Advantage:</strong> Unlike platforms that charge flat fees on *every single ticket*, our system calculates the fee on the <strong>entire checkout order total</strong>. Our minimum processing cushion protects you on multi-ticket group and family purchases.
            </div>

            <p className="text-slate-400 text-[10px] italic pt-1">
                Rates synced securely via active query nodes. Minimum fees apply per checkout order basket, not per individual seat item.
            </p>
        </div>
      </div>
      
    </div>
  );
}
