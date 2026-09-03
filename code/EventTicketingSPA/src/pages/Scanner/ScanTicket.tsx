import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import axiosClient from "../../api/axiosClient";
import { Capacitor } from '@capacitor/core';
import { CapacitorBarcodeScanner } from '@capacitor/barcode-scanner';
import { BrowserMultiFormatReader } from '@zxing/browser';
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import { toast, Toaster } from "react-hot-toast";
import Footer from "../../components/Footer";


export default function ScanTicket() {
  const { eventId } = useParams<{ eventId: string }>();
  const params = new URLSearchParams(location.search);
  const eventName = params.get("eventName") || "";

  const [isScanning, setIsScanning] = useState(false);
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [stopScan, setStopScan] = useState(false);
  const [manualCode, setManualCode] = useState("");
  const [webScanning, setWebScanning] = useState(false);
  const [cameraStarted, setCameraStarted] = useState(false);
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const browserReaderRef = useRef<BrowserMultiFormatReader | null>(null);
  const scanSessionIdRef = useRef(0);
  const isScannerActiveRef = useRef(false);

  const stopWebScanner = () => {
    isScannerActiveRef.current = false;
    scanSessionIdRef.current += 1;
    setIsScanning(false);

    const browserReader = browserReaderRef.current as BrowserMultiFormatReader & {
      stopContinuousDecode?: () => void;
      stopAsyncDecode?: () => void;
    } | null;

    browserReader?.stopContinuousDecode?.();
    browserReader?.stopAsyncDecode?.();

    if (videoRef.current) {
      const stream = videoRef.current.srcObject as MediaStream | null;
      stream?.getTracks().forEach((track) => track.stop());
      videoRef.current.srcObject = null;
    }

    browserReaderRef.current = null;
    setWebScanning(false);
    setCameraStarted(false);
    setStopScan(true);
  };

  const handleValidatedCode = async (code: string) => {
    if (!code || !eventId) {
      setScanResult("Invalid code or event ID");
      return;
    }

    const validationResult = await validateTicket(code);
    const isSuccess = validationResult === "Success" || validationResult?.status === "success" || validationResult?.message === "Success";

    setScanResult(typeof validationResult === "string" ? validationResult : JSON.stringify(validationResult));

    if (isSuccess) {
      toast.success("Ticket Validated", { position: "bottom-center" });
    } else {
      toast.error(typeof validationResult === "string" ? validationResult : "Validation failed", { position: "bottom-center" });
    }
  };

  const validateTicket = async (code: string) => {
    if (!code || !eventId) {
      return "Invalid code or event ID";
    }

    try {
      const result = await axiosClient.post(`/ticket/validate/${Number(eventId)}`, { qrCode: code });
      return result.data;
    } catch (error) {
      console.log("Validation error:", error);
      return "Validation failed";
    }
  };

  const startScan = async () => {
    setIsScanning(true);
    setScanResult(null);
    isScannerActiveRef.current = true;
    const sessionId = ++scanSessionIdRef.current;

    try {
      if (Capacitor.isNativePlatform()) {
        document.querySelector("body")?.classList.add("barcode-scanner-active");
        const result = await CapacitorBarcodeScanner.scanBarcode({ hint: 0 });

        if (result && result.ScanResult) {
          await handleValidatedCode(result.ScanResult);
        } else {
          setScanResult("Failed to scan");
          toast.error("Failed to scan", { position: "bottom-center" });
        }
      } else {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
          throw new Error("This browser does not support camera scanning.");
        }

        const browserReader = new BrowserMultiFormatReader();
        browserReaderRef.current = browserReader;

        if (!videoRef.current) {
          throw new Error("Camera preview is not ready.");
        }

        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: { ideal: "environment" } },
          audio: false,
        });

        videoRef.current.srcObject = stream;
        await videoRef.current.play();

        setWebScanning(true);
        setCameraStarted(true);

        browserReader.decodeFromVideoDevice(undefined, videoRef.current, async (result, error) => {
          if (!isScannerActiveRef.current || sessionId !== scanSessionIdRef.current) {
            return;
          }

          if (result) {
            stopWebScanner();
            console.log(`Code from scanner is ${result.getText()}`)
            await handleValidatedCode(result.getText());
            setIsScanning(false);
            setStopScan(false);
            setTimeout(() => {
              void startScan();
            }, 1200);
            return;
          }

          const isNoCodeFound =
            error &&
            typeof error === "object" &&
            "name" in error &&
            (error as { name?: string }).name === "NotFoundException";

          if (!isNoCodeFound) {
            console.warn("Web scan warning:", error);
          }
        });
      }
    } catch (error) {
      setStopScan(true);
      const message = error instanceof Error ? error.message : String(error);
      toast.error(message, { position: "bottom-center" });
      setScanResult(message);
    } finally {
      if (Capacitor.isNativePlatform()) {
        setIsScanning(false);
        document.querySelector("body")?.classList.remove("barcode-scanner-active");
      }
    }
  };

  const handleManualSubmit = async (providedCode?: string) => {
    const code = (providedCode ?? manualCode).trim();
    if (!code) {
      toast.error("Enter a ticket code first", { position: "bottom-center" });
      return;
    }

    setManualCode(code);
    stopWebScanner();
    setIsScanning(false);
    setStopScan(false);
    await handleValidatedCode(code);
  };

  useEffect(() => {
    return () => {
      stopWebScanner();
      if (Capacitor.isNativePlatform()) {
        document.querySelector("body")?.classList.remove("barcode-scanner-active");
      }
    };
  }, []);

  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col min-h-full">
          <Toaster
            containerStyle={{
              top: "calc(16px + env(safe-area-inset-top))",
              bottom: "calc(16px + env(safe-area-inset-bottom))",
            }}
          />

          <div className="max-w-md mx-auto mt-6 w-full px-4">
            <div className="flex flex-col items-center justify-between gap-4">
              <h2 className="text-xl font-semibold mb-2 text-center">Scan Ticket for Event {eventName}</h2>

              <button
                onClick={async () => {
                  if (Capacitor.isNativePlatform()) {
                    if (stopScan) {
                      setStopScan(false);
                      await startScan();
                      return;
                    }

                    stopWebScanner();
                    setStopScan(true);
                    return;
                  }

                  if (cameraStarted || webScanning || isScanning) {
                    stopWebScanner();
                    setStopScan(true);
                    return;
                  }

                  setStopScan(false);
                  await startScan();
                  
                }}
                className="bg-blue-600 text-center text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
              >
                {Capacitor.isNativePlatform()
                  ? stopScan ? "Start Scan" : "Stop Scan"
                  : cameraStarted || webScanning || isScanning ? "Stop Scan" : "Start Camera Scan"}
              </button>

              {!Capacitor.isNativePlatform() && (
                <div className="w-full rounded border border-slate-200 bg-white p-3 shadow-sm">
                  <div className="mb-2 text-sm font-medium text-slate-700">Web camera fallback</div>
                  <video
                    ref={videoRef}
                    className={cameraStarted ? "block h-64 w-full rounded bg-black object-cover" : "hidden"}
                    playsInline
                    muted
                  />
                      {webScanning && <div className="mb-2 mt-2 text-xs text-slate-500">Point your camera at the QR code</div>}
                  {!webScanning && !cameraStarted && !isScanning && (
                    <div className="mb-2 mt-2 text-xs text-slate-500">Camera ready. Tap Start Camera Scan when you are ready.</div>
                  )}

                  <div className="mt-3 flex gap-2">
                    <input
                      type="text"
                      value={manualCode}
                      onChange={(e) => setManualCode(e.target.value)}
                      placeholder="Or type ticket code"
                      className="w-full rounded border border-slate-300 px-3 py-2 text-sm"
                    />
                    <button
                      type="button"
                      onClick={() => void handleManualSubmit()}
                      className="rounded bg-slate-900 px-3 py-2 text-sm font-semibold text-white"
                    >
                      Validate
                    </button>
                  </div>

                  <div className="mt-3">
                    <button
                      type="button"
                      onClick={() => void handleManualSubmit("TESTTICKET_XF6OJJ1I")}
                      className="w-full rounded bg-emerald-600 px-3 py-2 text-sm font-semibold text-white"
                    >
                      Test sample ticket code
                    </button>
                  </div>
                </div>
              )}

              {scanResult && (
                <div className="text-center mt-2">
                  <p>{scanResult}</p>
                </div>
              )}
            </div>
          </div>

          <Footer />
        </div>
      </IonContent>
    </IonPage>
  );
}