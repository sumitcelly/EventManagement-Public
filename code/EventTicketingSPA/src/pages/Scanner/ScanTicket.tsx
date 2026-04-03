import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import { Capacitor } from '@capacitor/core';
import {CapacitorBarcodeScanner}  from '@capacitor/barcode-scanner';
import { IonContent, IonHeader, IonPage, IonButton, IonText, IonSpinner, IonToast } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import {toast, Toaster } from "react-hot-toast";
import Footer from "../../components/Footer";


export default function ScanTicket() {
  const { eventId } = useParams<{ eventId: string }>();
  const params = new URLSearchParams(location.search);
 // console.log('params',params,params.get(''))
  const eventName = params.get("eventName") || "";

  const [isScanning, setIsScanning] = useState(false);
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [stopScan, setStopScan] = useState(false);
  

  const validateTicket = async (code: string) => {
    if (!code || !eventId) {
      return "Invalid code or event ID";
    }
    try {
      const result = await axiosClient.post(`/ticket/validate/${Number(eventId)}`, 
      { qrCode: code });

      return result.data;// Adjust based on your API response
    } catch (error) {
      console.log('Validation error:', error);
      return "Validation failed";
    }
  };

  const startScan = async () => {
    setIsScanning(true);
    setScanResult(null);
    try {
      if (Capacitor.isNativePlatform()) {
        document.querySelector('body')?.classList.add('barcode-scanner-active');
        const result = await CapacitorBarcodeScanner.scanBarcode({ hint: 0 });
        if (result && result.ScanResult) {
          const validationResult = await validateTicket(result.ScanResult);
          setScanResult(validationResult);
          if (validationResult=="Success")
            toast.success("Ticket Validated",{position: 'bottom-center'});
          else
            toast.error(validationResult,{position: 'bottom-center'});
       

        } else {
        
         
          setScanResult("Failed to scan"+result.ScanResult);
          toast.error("Failed to scan",{position: 'bottom-center'});
        }
        //startScan();    
      } else {
        // Web fallback
        const validationResult = await validateTicket('XF6OJJ1I'); // Test code
        setScanResult(validationResult);
        
      }
    } catch (error) {
    
      setStopScan(true);
      toast.error(String(error),{position: 'bottom-center'});
      setScanResult(String(error));
      
    } finally {
      setIsScanning(false);
      if (Capacitor.isNativePlatform()) {
        document.querySelector('body')?.classList.remove('barcode-scanner-active');
      }
    }
  };

  useEffect(() => {
     
    let tempId:number;
    if(!isScanning && !stopScan)
    {
        tempId =setTimeout(() => {
            startScan();
        }, 2000);
       
    }
    return () => {
        // It stops the scanner from opening if the user leaves the page
        if (tempId) {
            clearTimeout(tempId);
        }
    };
    }
    , [isScanning, stopScan]);

  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col  min-h-full"> 
        <Toaster
            containerStyle={{
                // Ensure toasts stay below notches and above home bars
                top: 'calc(16px + env(safe-area-inset-top))',
                bottom: 'calc(16px + env(safe-area-inset-bottom))',
            }}
        />

        <div className="max-w-md mx-auto mt-6">
        <div className="flex flex-col items-center justify-between ">
            <h2 className="text-xl font-semibold mb-4 text-center">Scan Ticket for Event {eventName}</h2>
            <div>
            <button onClick={()=>{ 
                setStopScan(!stopScan); 
            }} 
                className="bg-blue-600 text-center text-white px-4 py-2 rounded hover:bg-blue-700 
                        disabled:bg-gray-400">
            {!stopScan ? "Stop Scan" : "Start Scan"}
            </button>

            </div>
            {scanResult && (
            <div className="text-center mt-4">
                <p>{scanResult}</p>
            </div>     
            )}
        </div>
           
       
      </div>
      <Footer/>
    </div>
    </IonContent>
    </IonPage>
  );
}