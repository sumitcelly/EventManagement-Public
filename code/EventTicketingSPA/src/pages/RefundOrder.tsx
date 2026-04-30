import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { IonHeader, useIonRouter } from "@ionic/react";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { IonContent, IonPage, IonRouterLink } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import { useParams, useLocation } from "react-router";
import { useState } from "react";
import toast, { Toaster } from "react-hot-toast";
import Footer from "../components/Footer";
// 
interface UserSalesOrder {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventSummary: string;
  eventOrganizer: number;
  eventLocation: string;
  salesOrderCode: string;
  eventHeadline:string;
}


  
export default function RefundOrder() {
  //const navigate = useNavigate();

  const [isChecked, setIsChecked] = useState(false);
  const [invalidRefundMode, setInvalidRefundMode] = useState(false);
  const router = useIonRouter();
  const  user = useAppSelector((state:RootState) => state.auth);
  const userId= user.user?.id;
  //const { id } = useParams<{ id: string }>();
  interface MyLocationState {
  orderId: string;
  eventId: string;
  orderTotal: number;
  salesTax:number;
  totalFees:number;
}

  const location = useLocation();
  const [refundStatus,setRefundStatus] = useState('');
  const {orderId, eventId,orderTotal, salesTax,totalFees} = location.state as MyLocationState || {};
  const refundAmount = orderTotal-totalFees;
  
  const handleCheckboxChange = (event:any) => {
    setIsChecked(event.target.checked);
  }

  const InitiateRefund =async () =>{
    if (refundAmount>0)
    {
      try
      {
        const res = await axiosClient.post(`/Payment/RefundOrder/${orderId}/${user?.user?.email}`);
        if (res.status ==200)
        {
          toast.success(`Refund initiated succefully`);
          setRefundStatus(`Refund initiated succefully. You will receive an email confirmation with the details at ${user?.user?.email}.`);
        }
        else
        {
          toast.error(`Error initiating refund`);
          setRefundStatus(`There was in issue with initiating your refund. We will get back to you after investigating it.`);
        }
      }
      catch(error:any)
      {
        setRefundStatus(`There was an error with initiating your refund. We will get back to you after investigating it.`);
        toast.error("There was an error refunding your order.Please try again." + error.message);
        console.log("Error refunding order. Please try again."+error.message);
      }
    }
    else
    {
      toast.error("Unable to initiate refund due to invalid refund mode or amount");
    }
  }
    

  if (refundAmount <=0)
  {
    return (
          <IonPage>
            <IonHeader>
              <AppNavbar />
            </IonHeader>
            <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
              
               <h2 className="text-xl font-bold mb-2">Refund amount is 0. Unable to proceed</h2>
                    
            </IonContent>
          </IonPage>
        );
  }


  return (
     <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col  min-h-full"> 
         <Toaster position="top-right" />
          <div className="max-w-md mx-auto mt-6">
            <h2 className="text-xl font-semibold mb-4 text-center">Refund Order</h2>
            <div className="mt-2 text-accent-dark text-sm font-body space-y-2">
              <div className="text-primary-color text-xl text-center">Your refund details</div>
               <div className="flex justify-between">
                <span> Order Items Total</span>
                <span>${(orderTotal-totalFees - salesTax).toFixed(2)}</span>
              </div>
               <div className="flex justify-between">
                <span> Sales Tax</span>
                <span>${salesTax.toFixed(2)}</span>
              </div>
               <div className="flex justify-between">
                <span> Refund Amount</span>
                <span>${refundAmount.toFixed(2)}</span>
              </div>
            </div>
            <div className="flex items-center gap-x-3 mt-2">
              <input type="checkbox" 
              className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-600"
              onClick={handleCheckboxChange} />
              <label className="text-sm font-medium text-gray-900">Check this box if you agree to receive a refund of ${refundAmount.toFixed(2)}</label>
            </div>
            <div className="mt-3 ml-auto flex">
              <button onClick={()=>InitiateRefund()}
                      className="ml-auto bg-blue-600 text-white px-4 py-1 rounded hover:bg-blue-700 disabled:bg-gray-400"
                      disabled={!isChecked || refundStatus.length > 0}
                      >
                      Initiate refund
              </button>
            </div>
            {refundStatus && (
              <div className="text-l mt-3  text-secondary-color">{refundStatus}</div>
            )}
          </div>
        <Footer/>
      </div>
    </IonContent>
    </IonPage>
  );
}
