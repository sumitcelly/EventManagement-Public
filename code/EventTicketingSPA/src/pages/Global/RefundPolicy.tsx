import React from 'react';
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import Footer  from '../../components/Footer';
import AppNavbar from '../../components/Navbar';
import { usePricingDetails } from '../../utils/PricingQuery';
import { useCompanyDetails } from '../../utils/CompanyQuery';


export function RefundPolicy() {
const { data: pricingData, isLoading, error } = usePricingDetails(0);
const  {data: companyData, isLoading: companyLoading, error:companyError} = useCompanyDetails();

if (isLoading || companyLoading) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-slate-50 text-slate-900 text-xs rounded-xl border border-slate-100">
        Loading pricing and company details...
      </div>
    );
  }

  if (error || !pricingData) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-red-50 text-red-700 text-xs rounded-xl border border-red-100">
        ⚠️ Failed to synchronize live pricing  structures. Standard platform base rates apply.
      </div>
    );
  }
   if (companyError || !companyData) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-red-50 text-red-700 text-xs rounded-xl border border-red-100">
        ⚠️ Failed to synchronize live company data.
      </div>
    );
  }
  return (
     <IonPage>
        <IonHeader>
            <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    
        <div className="min-h-screen bg-slate-50 py-12 px-4 sm:px-6 lg:px-8 flex items-center justify-center">
        <div className="max-w-2xl w-full bg-white rounded-2xl border border-slate-100 shadow-xl p-6 sm:p-8 flex flex-col space-y-5 text-sm text-slate-700 leading-relaxed">
            
            {/* Page Header */}
            <div className="border-b border-slate-100 pb-4">
            <div className="flex items-center space-x-2 text-indigo-600 mb-1">
                <span className="text-xl">↩️</span>
                <span className="text-xs font-bold uppercase tracking-wider">Legal Framework</span>
            </div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Ticket Purchase & Refund Policy</h1>
            <p className="text-xs text-slate-400 mt-1">Last Updated: July 2026</p>
            </div>

            {/* Introduction */}
            <p>
            This platform provides proprietary event management and ticket issuance software services. By purchasing a ticket or completing a transaction loop through this system, you explicitly acknowledge, understand, and agree to the financial processing parameters outlined below.
            </p>

            {/* Crucial Financial Disclosure Box */}
            <div className="bg-amber-50 rounded-xl p-4 border border-amber-200 flex flex-col space-y-2 text-xs text-amber-950">
            <p className="font-bold flex items-center space-x-1">
                <span>🚨</span>
                <span>CRUCIAL FINANCIAL DISCLOSURE:</span>
            </p>
            <p>
                All ticket transaction revenue routes natively via <strong>Stripe Connect Standard</strong> network nodes. This software platform never collects, manages, holds, or retains ticket buyer funds. One hundred percent (100%) of your ticket's face value jumps instantly and directly into the independent <strong>Event Organizer's personal or business Stripe account balance</strong>.
            </p>
            </div>

            {/* Section 1: Organizer Settings Clause */}
            <div className="space-y-2">
            <h3 className="font-bold text-base text-slate-900 flex items-center space-x-2">
                <span className="text-sm">1.</span>
                <span>Organizer-Controlled Refund Rules</span>
            </h3>
            <p>
                Because the platform does not hold or manage your ticket funds, <strong>the individual Event Organizer maintains complete sole legal and financial authority over refund rules and approvals</strong>. 
            </p>
            <p>
                During event creation, organizers choose whether to toggle <strong>Self-Service Refunds</strong> on or off. Refunds are cutoff {companyData.refundCutoff} hours prior to the event.
            </p>
            </div>

            {/* Section 2: Automated Self-Service Tool */}
            <div className="space-y-2">
            <h3 className="font-bold text-base text-slate-900 flex items-center space-x-2">
                <span className="text-sm">2.</span>
                <span>How to Claim a Refund</span>
            </h3>
            <p>
                If the Event Organizer has enabled automated returns for their specific listing, a prominent <strong>"Initiate Refund"</strong> option will be visible on your digital ticket confirmation page. Clicking this button triggers a secure backend API call that instantly reverses the transaction through Stripe's banking network.
                The customer will receive an email with the refund details.
            </p>
            <p>
                If the organizer has disabled automated refunds, the automated button will vanish. In this scenario, you must contact the organizer directly using the support links provided on your ticket order to request a manual evaluation.
            </p>
            </div>

            {/* Section 3: The Non-Refundable Fees Shield */}
            <div className="space-y-2 bg-slate-50 border border-slate-100 rounded-xl p-4">
            <h3 className="font-bold text-sm text-slate-900 uppercase tracking-wide text-indigo-950">
                ⚠️ 3. Non-Refundable Processing Surcharges
            </h3>
            <p className="text-xs text-slate-600 pt-1">
                When a refund is successfully processed—whether automatically via the self-service button or manually by the organizer—the following transaction cost rules apply strictly:
            </p>
            <ul className="text-xs space-y-2 text-slate-700 pl-4 list-disc pt-1">
                <li>
                <strong>Platform Application Fees:</strong> The {pricingData.platformFees * 100}% platform cut (or the ${(pricingData.floor / 100).toFixed(2)} order minimum floor) covers the non-refundable costs of database write validation, asset hosting, and automated AWS SES email message delivery. <strong>This fee component is strictly non-refundable.</strong>
                </li>
                <li>
                <strong>Stripe Merchant Processing Fees:</strong> Credit card processing networks (Visa, Mastercard, etc.) and Stripe retain their standard transaction service fees ({(pricingData.stripeFees * 100).toFixed(2)}% + {(pricingData.stripeFixed)} cents ) on processed checkouts. <strong>These payment network fees are strictly non-refundable.</strong>
                </li>
            </ul>
            <p className="text-[11px] text-slate-400 italic pt-1 border-t border-slate-200/60 mt-2">
                *Result:* The ticket buyer will receive a refund equal to the exact face value of the ticket item, minus the original transactional processing and software platform cuts.
            </p>
            </div>

            {/* Back Button / Closing Hook */}
            <div className="pt-2 text-center">
            <p className="text-xs text-slate-400 mb-4">
                For system architecture audits or integration status verification, message Platform Support.
            </p>
            <button 
                onClick={() => window.history.back()}
                className="w-full sm:w-auto inline-flex justify-center items-center px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white font-semibold rounded-xl text-xs shadow-sm transition-colors duration-150"
            >
                ← Return to Event View
            </button>
            </div>

        </div>

        </div>
        </IonContent>
        <Footer/>
    </IonPage>
    )
}
