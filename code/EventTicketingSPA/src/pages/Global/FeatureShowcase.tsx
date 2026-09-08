import React, { useState } from 'react';
import { usePricingDetails } from '../../utils/PricingQuery';
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from '../../components/Navbar';
import Footer from "../../components/Footer";

export function FeaturesShowcase() {
  const { data: pricingData, isLoading, error } = usePricingDetails(0);
    // Manage active tab state ('organizer' vs 'buyer')
  const [activeTab, setActiveTab] = useState<'organizer' | 'buyer'>('organizer');

    if (isLoading) {
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


  // Organizer Feature List Mapping
  const organizerFeatures = [
    {
      icon: "📊",
      title: "Hybrid Per-Ticket Pricing",
      description: `A transparent platform fee of ${pricingData.platformFees * 100}%  or a minimum of $${(pricingData.floor/100).toFixed(2)} cents per ticket—whichever amount is greater. This keeps our system highly competitive for standard events while safely covering processing costs on micro-tickets.`
    },
    {
      icon: "💳",
      title: "Direct Stripe Integration",
      description: "Connect your existing personal or business Stripe account natively. View your complete real-time Stripe connection status and configuration metrics right inside your dashboard."
    },
    {
      icon: "🎟️",
      title: "Timed-Entry Ticketing",
      description: "Control venue crowd flow flawlessly. Schedule exact entry time-slots and cap capacity limits per window to eliminate gate bottlenecks."
    },
    {
      icon: "↩️",
      title: "Flexible Refund Toggles",
      description: "Take control of cancellations. Choose to activate our self-service refund button to let buyers claim automated reversals instantly, or disable it to process returns manually."
    },
    {
      icon: "⚖️",
      title: "Fee Capital Absorption",
      description: "Decide exactly who pays the processing costs. Pass 100% of the software and merchant fees onward to the buyer, or choose to absorb the Stripe component to reward your fans."
    },
    {
      icon: "🧪",
      title: "End-to-End Checkout Simulation",
      description: "Run secure sandbox checkouts before publishing live. Use fake testing credit cards to verify that your email delivery networks and scanning systems work perfectly."
    },
    {
      icon: "📈",
      title: "Live Order Status Reports",
      description: "Audit your transaction records instantly. Track real-time event revenue totals, remaining ticket inventory maps, and detailed attendee email rosters."
    },
    {
      icon: "📧",
      title: "Automated Lifecycle Reminders",
      description: "Our server queues handle event reminders completely. Attendees automatically receive rich text informational update campaigns exactly 7 days and 2 days before doors open."
    },
    {
      icon: "✉️",
      title: "Direct Attendee Broadcasts",
      description: "Keep your crowds informed. Compose and broadcast custom email announcements or emergency schedule changes directly to your entire attendee roster with one click."
    },
    {
      icon: "👥",
      title: "Granular Team Delegation",
      description: "Create and manage your event staff securely. Assign specialized roles like 'Scanning Agent' (unlocked browser camera scanning) or 'Restricted Admin' to delegate tasks safely."
    }
  ];

  // Buyer Feature List Mapping
  const buyerFeatures = [
    {
      icon: "⚡",
      title: "Frictionless Guest Checkout",
      description: "No annoying usernames, passwords, or mandatory profile setups required. Buyers select tickets, execute secure checkout, and get their codes in seconds."
    },
    {
      icon: "📱",
      title: "Instant Mobile Ticket Access",
      description: "Receive printable ticket passes and cryptographic scanning barcodes directly in your email inbox immediately after payment finishes."
    },
    {
      icon: "🔄",
      title: "Self-Service Cancellations",
      description: "If enabled by the event host, request a secure automated refund directly from your digital order confirmation page without waiting on customer support."
    }
  ];

  return (
     <IonPage>
        <IonHeader>
            <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
        <div className="w-full bg-slate-50 py-16 px-4 sm:px-6 lg:px-8 border-t border-slate-200/60">
        <div className="max-w-6xl mx-auto space-y-10">
        
        {/* Header Content */}
        <div className="text-center space-y-3">
          <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight sm:text-4xl">
            Engineered for Exceptional Event Experiences
          </h2>
          <p className="text-slate-500 text-sm max-w-xl mx-auto">
            A lean, secure, and modern cloud platform optimized to maximize revenue for organizers while delivering ultra-fast checkouts for attendees.
          </p>
        </div>

        {/* Tab Toggle Navigation Bar */}
        <div className="flex justify-center">
          <div className="bg-slate-200/70 p-1 rounded-xl flex space-x-1 border border-slate-200">
            <button
              onClick={() => setActiveTab('organizer')}
              className={`px-5 py-2 text-sm font-semibold rounded-lg transition-all duration-150 ${
                activeTab === 'organizer'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              🛠️ For Event Organizers
            </button>
            <button
              onClick={() => setActiveTab('buyer')}
              className={`px-5 py-2 text-sm font-semibold rounded-lg transition-all duration-150 ${
                activeTab === 'buyer'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              🎉 For Ticket Buyers
            </button>
          </div>
        </div>

        {/* Dynamic Grid Layout Wrapper */}
        <div className="pt-4">
          {activeTab === 'organizer' ? (
            /* Organizer Features Grid */
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 animate-in fade-in duration-300">
              {organizerFeatures.map((feat, index) => (
                <div 
                  key={index} 
                  className="bg-white p-5 rounded-2xl border border-slate-100 shadow-md hover:shadow-lg hover:border-slate-200/80 transition-all flex flex-col space-y-3"
                >
                  <div className="inline-flex items-center justify-center h-10 w-10 rounded-xl bg-indigo-50 text-xl">
                    {feat.icon}
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-slate-900">{feat.title}</h3>
                    <p className="text-xs text-slate-500 mt-1 leading-relaxed">{feat.description}</p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            /* Buyer Features Grid */
            <div className="max-w-4xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-6 animate-in fade-in duration-300">
              {buyerFeatures.map((feat, index) => (
                <div 
                  key={index} 
                  className="bg-white p-5 rounded-2xl border border-slate-100 shadow-md hover:shadow-lg hover:border-slate-200/80 transition-all flex flex-col space-y-3"
                >
                  <div className="inline-flex items-center justify-center h-10 w-10 rounded-xl bg-emerald-50 text-xl">
                    {feat.icon}
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-slate-900">{feat.title}</h3>
                    <p className="text-xs text-slate-500 mt-1 leading-relaxed">{feat.description}</p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Platform Call to Action Notice Footer */}
        <div className="bg-slate-950 rounded-2xl p-6 md:p-8 text-white flex flex-col md:flex-row items-center justify-between shadow-xl border border-slate-800 space-y-4 md:space-y-0 text-center md:text-left">
          <div className="space-y-1">
            <h4 className="text-base font-bold tracking-tight text-indigo-400">Ready to transform your ticketing system?</h4>
            <p className="text-slate-400 text-xs max-w-md">
              Form your event structure under our secure <strong>SimplyTickets LLC</strong> compliance protocols with zero setup expenses [finance].
            </p>
          </div>
          <div className="flex flex-col sm:flex-row space-y-2 sm:space-y-0 sm:space-x-3 w-full md:w-auto">
            <button 
              onClick={() => window.location.href = '/signup'}
              className="w-full sm:w-auto px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 font-semibold rounded-xl text-xs tracking-wide shadow-md transition-colors"
            >
              Get Started for Free
            </button>
          </div>
        </div>

      </div>
      <Footer/>
    </div>
    </IonContent>
      
    </IonPage>
    
  );
}
