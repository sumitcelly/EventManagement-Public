import { useQuery } from 'react-query';
import axiosClient from '../api/axiosClient';
import { PricingDetails } from "../utils/PricingQuery";



interface PricingTableProps {
  eventId?: number; // Optional lookup parameter for custom organizer overrides
}

export function PricingTable({ eventId }: PricingTableProps) {

  const { data: pricingData, isLoading, error } = PricingDetails(eventId);
  
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
  if (error || !pricingData) {
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
                     Platform fee of {pricingData.platformFees * 100}%  of the total order or a minimum of ${(pricingData.floor/100).toFixed(2)} cents per ticket—whichever amount is greater.
                    </span>
                  </td>
                  <td className="p-3 text-right font-mono text-xs font-semibold text-indigo-600 leading-tight">
                    <span className="text-base font-bold">{pricingData.platformFees*100}%</span> or
                    <span className="block font-sans text-[10px] text-slate-400 font-normal mt-0.5">(${(pricingData.floor/100).toFixed(2)} per ticket minimum)</span>
                  </td>
               </tr>

              {/* Row 3: Card Processor Cut */}
              <tr className="hover:bg-slate-50/50 transition-colors">
                <td className="p-3 font-medium text-slate-900">
                  Stripe Merchant Fee
                  <span className="block text-xs font-normal text-slate-400 mt-0.5">Standard card rate charged directly by credit payment networks</span>
                </td>
                <td className="p-3 text-right font-mono font-semibold text-slate-900">
                  {(pricingData.stripeFees*100).toFixed(2)}% + {formatCents(pricingData.stripeFixed)}
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
                🚀 <strong>Capped Micro-Ticket Fees:</strong> Unlike legacy platforms that stack heavy, per-ticket surcharges, our processing rate is only {pricingData.platformFees*100}% of the total order value, or a minimum of ${(pricingData.floor/100).toFixed(2)} per ticket— <strong>whichever is higher</strong>.
                This structural cap guarantees massive fee savings for multi-ticket group and family purchases.
            </div>

            <p className="text-slate-400 text-[10px] italic pt-1">
                Rates synced securely via active query nodes. Minimum fees apply per checkout order basket, not per individual seat item.
            </p>
        </div>
      </div>
      
    </div>
  );
}
