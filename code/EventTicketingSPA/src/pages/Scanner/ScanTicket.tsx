import { useState } from "react";
import { useParams } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import { Capacitor } from '@capacitor/core';
import {CapacitorBarcodeScanner}  from '@capacitor/barcode-scanner';
import { IonContent, IonHeader, IonPage, IonButton, IonText, IonSpinner, IonToast } from "@ionic/react";
import AppNavbar from "../../components/Navbarnew";
import {toast, Toaster } from "react-hot-toast";


export default function ScanTicket() {
  const { eventId } = useParams<{ eventId: string }>();
  const params = new URLSearchParams(location.search);
 // console.log('params',params,params.get(''))
  const eventName = params.get("eventName") || "";

  const [isScanning, setIsScanning] = useState(false);
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [showToast, setShowToast] = useState(false);
  const [toastMessage, setToastMessage] = useState("");

  const validateTicket = async (code: string) => {
    if (!code || !eventId) {
      return "Invalid code or event ID";
    }
    try {
      const result = await axiosClient.post('/ticket/validate', 
      { qrCode: code, eventId:Number(eventId) });

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
        if (result.ScanResult) {
          const validationResult = await validateTicket(result.ScanResult);
          setScanResult(validationResult);
          setToastMessage(validationResult);
          setShowToast(true);
        //   if (validationResult=="Success")
        //     toast.success("Ticket Validated");
        //   else
        //     toast.error(validationResult);

        } else {
          setScanResult("Failed to scan");
          setToastMessage("Failed to scan");
          setShowToast(true);
        }
      } else {
        // Web fallback
        const validationResult = await validateTicket('XF6OJJ1I'); // Test code
        setScanResult(validationResult);
        setToastMessage(validationResult);
        setShowToast(true);
      }
    } catch (error) {
      setScanResult("Scan error");
      setToastMessage("Scan error");
      
    } finally {
      setIsScanning(false);
      if (Capacitor.isNativePlatform()) {
        document.querySelector('body')?.classList.remove('barcode-scanner-active');
      }
    }
  };

  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
        <Toaster position="top-right" />
        <div className="max-w-md mx-auto mt-6">
        <div className="flex flex-col items-center justify-between ">
            <h2 className="text-xl font-semibold mb-4 text-center">Scan Ticket for Event {eventName}</h2>
            <div>
            <button onClick={startScan} disabled={isScanning}
                        className="bg-blue-600 text-center text-white px-4 py-2 rounded hover:bg-blue-700 
                        disabled:bg-gray-400">
            {isScanning ? <IonSpinner name="crescent" /> : "Start Scan"}
            </button>
            </div>
             {scanResult && (
            <div className="text-center mt-4">
                <p>{scanResult}</p>
            </div>
            )}
            <IonToast
                isOpen={showToast}
                onDidDismiss={() => setShowToast(false)}
                message={toastMessage}
                duration={3000}
            />
        </div>
           
       
        </div>
      </IonContent>
    </IonPage>
  );
}