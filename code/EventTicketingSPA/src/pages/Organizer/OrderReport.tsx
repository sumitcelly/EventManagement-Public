import { useQuery } from "react-query";
import axiosClient from "../../api/axiosClient";
import { useNavigate,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import * as DateUtils from "../../utils/DateUtils"
import toast, { Toaster } from "react-hot-toast";
import { useForm } from "react-hook-form";
import { use, useEffect } from "react";
// 


const memberSchema = yup.object({
  startDate: yup.string().default(DateUtils.addDaysToDate(new Date(), -30).toISOString().split('T')[0]).required("Start date is required."),
  endDate: yup.string().default(new Date().toISOString().split('T')[0]).required("End date is required."),
  email: yup.string().default(""),
  fullname: yup.string().default(""),
  orderStatus: yup.string().default(""),
  eventName: yup.string().default(""),
  }).test('start-end-date', 'Start date must be before end date', function(value) {
    const { startDate, endDate } = value;
    return startDate <= endDate;
  });

type FormValues = {
  startDate: string;
  endDate:string;
  email:string;
  fullname:string;
  orderStatus:string;
  eventName:string;
};

  
export default function OrderReport() {
  const navigate = useNavigate();
  const  user = useAppSelector((state:RootState) => state.auth);
  const customerId = user.user?.customerId;
  
  const { data:events, isLoading:isEventsLoading } = 
  useQuery(['EventsByCustomerId',customerId], async () => {
     console.log("Fetching events for customer", customerId);
     try
     {
      const res = await axiosClient.get(`/Events/ByCustomer/${customerId}`);
      if (res?.data && res.status===200)
      {
          console.log('events fetched from backend',res.data);
          return res.data;
      }
      else
      {
          console.log('events fetched from backend',res.data);
          return [];
      }
    }
    catch(error)
    {
        console.error("Error fetching events:", error);
        toast.error("Error loading report data");
        return [];
    }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!customerId //  only run query if we have an id
    }
  );


  let searchResults: any[] = [];

  const onSubmit = (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    const queryParams = new URLSearchParams();
    queryParams.append("startDate", data.startDate);
    queryParams.append("endDate", data.endDate);
    if (data.email)
      queryParams.append("emailAddress", data.email);
    if (data.fullname)
      queryParams.append("name", data.fullname);
    if (data.orderStatus)
      queryParams.append("orderStatus", data.orderStatus);
    if (data.eventName)
      queryParams.append("eventId", data.eventName);
    queryParams.append("isAscending","false");
    const queryString = queryParams.toString();
    console.log("Generated query string:", queryString);
    
    axiosClient.get(`/SalesOrderByCustomer/${customerId}?${queryString}`).then(response => {
      console.log('Order report data fetched successfully:', response.data);
      toast.success("Report data fetched. Check console log.");
      searchResults = response.data;
      // Handle the response data as needed
    }).catch(error => {
      console.error('Error fetching order report data:', error);
      toast.error("Error fetching report data");
      // Handle error (e.g., show notification to user)
    });

  }
  
    const {
      control,
      handleSubmit,
      reset,
      register,
      formState: { errors },
    } = useForm<FormValues>({
      resolver: yupResolver(memberSchema),
        mode: "onChange",          // 👈 validates as user types or changes field
        reValidateMode: "onChange"
    })
     
    useEffect(() => {
      reset({
        startDate: DateUtils.addDaysToDate(new Date(), -30).toISOString().split('T')[0],
        endDate: new Date().toISOString().split('T')[0],
        email: "",
        fullname: "",
        orderStatus: "",
        eventName: ""
      });
    }, [events, reset]);
    
    if (isEventsLoading) return <p>Loading...</p>;
  return (
    <form onSubmit={handleSubmit(onSubmit)}
      className="max-w-xl mx-auto p-3 border border-gray-300 rounded-lg shadow-lg bg-brand-neutral"
    >  
      <Toaster position="top-right" />
      <div className="text-center text-2xl font-bold text-accent-color mb-6">Order Report</div>

      {/* Date Row */}
      <div className="flex flex-row gap-x-4 mb-4">
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">Start Date</label>
          <input
            type="date"
            {...register("startDate")}
            className="w-full border rounded p-2"
          />
          <div className="min-h-[20px]">
            {errors.startDate && (
              <p className="text-red-600 text-sm mt-1">{errors.startDate.message}</p>
            )}
          </div>
        </div>
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">End Date</label>
          <input
            type="date"
            {...register("endDate")}
            className="w-full border rounded p-2"
          />
          <div className="min-h-[20px]">
            {errors.endDate && (
              <p className="text-red-600 text-sm mt-1">{errors.endDate.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Email/Name Row */}
      <div className="flex flex-row gap-x-4 mb-4">
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">Email</label>
          <input
            type="text"
            {...register("email")}
            className="w-full border rounded p-2"
          />
            <div className="min-h-[20px]"></div>
        </div>
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">Name</label>
          <input
            type="text"
            {...register("fullname")}
            className="w-full border rounded p-2"
          />
        </div>
          <div className="min-h-[20px]"> </div>
      </div>

      {/* Order Status/Event Name Row */}
      <div className="flex flex-row gap-x-4 mb-4">
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">Order Status</label>
          <select
            {...register("orderStatus")}
            className="w-full border rounded p-2"
          >
            <option value="">Any Status</option>
            <option value="PaymentPending">In Progress</option>
            <option value="PaymentRequired">Payment Required</option>
            <option value="PaymentInitiated">Payment Initiated</option>
            <option value="PaymentFailed">Payment Failed</option>
            <option value="PaymentSucceeded">Payment Succeeded</option>
            <option value="OrderCompleted">Order Completed</option>
            <option value="Refunded">Refunded</option>
          </select>
        </div>
        <div className="flex flex-col w-1/2 space-y-1">
          <label className="font-semibold">Event Name</label>
          <select
            {...register("eventName")}
            className="w-full border rounded p-2">
            <option value="">{"Select an option"}</option>
            {events.map((event:any) => (
              <option key={event.eventId} value={event.eventId}>
                {event.eventName}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Submit Button */}
      <div className="flex justify-end mt-4">
        <button
          type="submit"
          className="bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
        >
          Search
        </button>
      </div>
    </form>
  );
}
