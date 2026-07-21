import { IonContent, IonHeader, IonPage } from "@ionic/react";
import React, { useState } from 'react';
import AppNavbar from '../../components/Navbar';
import Footer from "../../components/Footer";
import { usePricingDetails } from "../../utils/PricingQuery";
import {useCompanyDetails} from "../../utils/CompanyQuery"

interface FAQItem {
  id: number;
  question: string;
  answer: React.ReactNode; // Allows passing rich HTML formatting inside the answer text
}

export default function HelpCenter() {
  // Track which accordion item ID is currently open (null means all closed)
  const [openId, setOpenId] = useState<number | null>(null);

  const { data: pricingData, isLoading, error } = usePricingDetails(0);
  const { data: companyData, isLoading:companyDetailsLoading,error:errorCompany } = useCompanyDetails();

  console.log('dat from help center', pricingData, isLoading, error);

  const toggleAccordion = (id: number) => {
    setOpenId(openId === id ? null : id);
  };

  if (isLoading || companyDetailsLoading) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-slate-50 text-slate-900 text-xs rounded-xl border border-slate-100">
        Loading pricing and company details...
      </div>
    );
  }

  if (error || errorCompany) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-red-50 text-red-700 text-xs rounded-xl border border-red-100">
        ⚠️ Failed to synchronize live pricing and company structures. Standard platform base rates apply.
      </div>
    );
  }

  if (!pricingData || !companyData) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-yellow-50 text-yellow-900 text-xs rounded-xl border border-yellow-100">
        ⚠️ Pricing and company details are unavailable at the moment.
      </div>
    );
  }

  const faqData: FAQItem[] = [
    {
      id: 1,
      question: "Where does the money from ticket sales go, and when do I get paid?",
      answer: <p>Your funds never sit in our corporate accounts. Because we utilize <strong>Stripe Connect Standard</strong> infrastructure, ticket revenue flows directly and immediately into your own personal or business Stripe ledger the exact millisecond a customer checks out.</p>
    },
    {
      id: 2,
      question: "How exactly do your platform fees work?",
      answer: <p>Our software service fee is a flat <strong>{pricingData.platformFees * 100}% of the total order basket value</strong>. To keep processing overhead secure for low-ticket microtransactions, the fee clamps to a minimum floor of just <strong>${(pricingData.floor / 100).toFixed(2)} total per checkout order</strong>. Standard Stripe payment network merchant processor fees {(pricingData.stripeFees * 100).toFixed(2)}% + {(pricingData.stripeFixed)} cents 
        apply natively alongside this cut.</p>
    },
    {
      id: 3,
      question: "Can I manage and delegate volunteer scanning staff?",
      answer: <p>Yes! Under your event management panel, you can create and manage dedicated gate crews. The system generates secure, isolated URLs you can text or email to volunteers, allowing them to instantly register arrivals under your supervision.</p>
    },
    {
      id: 4,
      question: "How do your automated attendee email notifications behave?",
      answer: <p>Communication is handled flawlessly by our system servers. Attendees receive an instantaneous receipt and barcode right after buying. Our automated queuing engine then sends highly optimized informational reminders exactly <strong>7 days</strong> and <strong>2 days</strong> before your event start time.</p>
    },
    {
      id: 5,
      question: "What hardware or app dependencies are required for gate check-ins?",
      answer: <p>Absolutely none. Any modern smartphone or tablet equipped with a working camera lens can scan tickets natively. Your volunteers simply open their assigned staff gateway link in their mobile browser (Safari, Chrome, etc.) and scan barcodes instantly.</p>
    },
    {
      id: 6,
      question: "Can I safely test my checkout, email, and scanning systems before publishing?",
      answer: <p>Yes, and we highly recommend it! Right on your event's final verification layout, you can trigger a <strong>Sandbox Simulation</strong>. This allows you to walk through the ticket-buying sheet, use a simulated testing credit card, receive a test barcode layout in your main email box, and practice scanning it with your smartphone.</p>
    }
  ];

 

  if (isLoading) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-slate-50 text-slate-900 text-xs rounded-xl border border-slate-100">
        Loading pricing details...
      </div>
    );
  }

  if (error) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-red-50 text-red-700 text-xs rounded-xl border border-red-100">
        ⚠️ Failed to synchronize live pricing structures. Standard platform base rates apply.
      </div>
    );
  }

  if (!pricingData) {
    return (
      <div className="w-full max-w-xl mx-auto p-4 bg-yellow-50 text-yellow-900 text-xs rounded-xl border border-yellow-100">
        ⚠️ Pricing details are unavailable at the moment.
      </div>
    );
  }

  return (
    <IonPage>
        <IonHeader>
            <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
            <div className="min-h-screen bg-slate-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-2xl mx-auto space-y-8">
                
                {/* Help Center Header text banner */}
                <div className="text-center space-y-2">
                <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight sm:text-4xl">Help Center & FAQ</h1>
                <p className="text-sm text-slate-500 max-w-md mx-auto">
                    Everything you need to know about setting up events, managing ticket distributions, and tracking your secure Stripe payouts.
                </p>
                </div>

                {/* Dynamic Accordion List Block */}
                <div className="bg-white rounded-2xl border border-slate-100 shadow-xl overflow-hidden divide-y divide-slate-100">
                {faqData.map((item) => {
                    const isOpen = openId === item.id;
                    return (
                    <div key={item.id} className="transition-colors duration-150">
                        
                        {/* Trigger Button Row */}
                        <button
                        onClick={() => toggleAccordion(item.id)}
                        className="w-full flex items-center justify-between p-5 text-left font-semibold text-slate-900 hover:bg-slate-50/80 transition-all focus:outline-none"
                        >
                        <span className="text-sm sm:text-base pr-4">{item.question}</span>
                        <span className={`text-xs transform transition-transform duration-200 text-slate-400 font-bold ${isOpen ? 'rotate-180 text-indigo-600' : ''}`}>
                            ▼
                        </span>
                        </button>

                        {/* Animated Body Reveal Panel */}
                        <div 
                        className={`overflow-hidden transition-all duration-200 ease-in-out ${
                            isOpen ? 'max-h-40 border-t border-slate-50 bg-slate-50/30' : 'max-h-0'
                        }`}
                        >
                        <div className="p-5 text-xs sm:text-sm text-slate-600 leading-relaxed">
                            {item.answer}
                        </div>
                        </div>

                    </div>
                    );
                })}
                </div>

                {/* Catch-all Help Desk Footer Hook */}
                <div className="bg-indigo-50/50 rounded-2xl p-5 border border-indigo-100 flex flex-col sm:flex-row sm:items-center sm:justify-between space-y-3 sm:space-y-0 text-center sm:text-left">
                <div className="space-y-0.5">
                    <h4 className="text-sm font-bold text-indigo-950">Still need a hand?</h4>
                    <p className="text-xs text-indigo-900/80">Can't find the answer you are hunting for? Send us a direct line.</p>
                </div>
                <a
                    href={`mailto:${companyData.supportEmail}`}
                    className="inline-flex justify-center items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-xl text-xs shadow-sm transition-all"
                >
                    ✉️ Email Platform Support
                </a>
                </div>

            </div>
            </div>
        </IonContent>
        <Footer/>
    </IonPage>
  );
}
