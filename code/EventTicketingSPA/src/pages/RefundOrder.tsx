import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { IonHeader, useIonRouter } from "@ionic/react";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../app/hook";
import { RootState } from "../app/store";
import { IonContent, IonPage, IonRouterLink } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
import { useParams, useLocation } from "react-router";
import { useState } from "react";
import toast, { Toaster } from "react-hot-toast";
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
}

  const location = useLocation();
  const {orderId, eventId} = location.state as MyLocationState || {};

  const { data:refundAmount, isLoading } = 
  useQuery(['SalesOrderRefundAmount',orderId], async () => {
     console.log("Fetching refund amount for order", orderId);
      try
      {
        const res = await axiosClient.get(`/SalesOrderRefundAmount/${orderId}/${eventId}`);
        console.log('amount fetched from backend from backend',res);
        if (res.status !=200)
        {
          toast.error('Unable to initiate refund:', res.data);
          setInvalidRefundMode(true);
        }
        return res.data;
      }
      catch(error)
      {
        console.log('error getting refund amount', error);
        setInvalidRefundMode(true);
      }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!orderId //  only run query if we have an id
    }
  );

  if (isLoading) return <p>Loading...</p>;
  const handleCheckboxChange = (event:any) => {
    setIsChecked(event.target.checked);
  }

  const InitiateRefund =async () =>{
    if (Number(refundAmount) >0 && !invalidRefundMode)
    {
      try
      {
        const res = await axiosClient.post(`/Payment/RefundOrder/${orderId}`);
        if (res.status ==200)
        {
          toast.success(`Refund initiated succefully`);
        }
        else
        {
          toast.error(`Error initiating refund`);
        }
      }
      catch(error:any)
      {
        toast.error("There was an error refunding your order.Please try again." + error.message);
        console.log("Error refunding order. Please try again."+error.message);
      }
    }
    else
    {
      toast.error("Unable to initiate refund due to invalid refund mode or amount");
    }
  }
    

  if (Number(refundAmount) <=0)
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

  if (invalidRefundMode)
  {
    return (
          <IonPage>
            <IonHeader>
              <AppNavbar />
            </IonHeader>
            <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
              
               <h2 className="text-xl font-bold mb-2">Refund mode is invalid.</h2>
                    
            </IonContent>
          </IonPage>
        );
  }
  return (
     <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent  className="ion-padding flex flex-col justify-center items-center h-full">
         <Toaster position="top-right" />
          <div className="max-w-md mx-auto mt-6">
            <h2 className="text-xl font-semibold mb-4">Refund Order</h2>
            <div className="flex items-center gap-x-3">
              <input type="checkbox" 
              className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-600"
              onClick={handleCheckboxChange} />
              <label className="text-sm font-medium text-gray-900">Check this box if you agree to receive a refund of {refundAmount}</label>
            </div>
            <div className="ml-auto mt-3">
              <button onClick={()=>InitiateRefund()}
                      className="ml-auto bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-gray-400"
                      disabled={!isChecked}>
                      Initiate refund
              </button>
            </div>
          </div>
    </IonContent>
    </IonPage>
  );
}
