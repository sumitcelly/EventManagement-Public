import { useInfiniteQuery, useQuery } from "react-query";
import axiosClient, { API_BASE_URL } from "../../api/axiosClient";
import { useHistory,Link } from "react-router-dom";
import { ListGroup, ListGroupItem, Button, Checkbox} from "flowbite-react";
import { useAppSelector } from "../../app/hook";
import { RootState } from "../../app/store";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import * as DateUtils from "../../utils/DateUtils"
import toast, { Toaster } from "react-hot-toast";
import { set, useForm } from "react-hook-form";
import { use, useEffect, useState } from "react";
import React from "react";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../../components/Navbar";
import Footer from "../../components/Footer";

// 

const pageSize =10;
const memberSchema = yup.object({
  startDate: yup.string().default(DateUtils.addDaysToDate(new Date(), -30).toISOString().split('T')[0]).required("Start date is required."),
  endDate: yup.string().default(new Date().toISOString().split('T')[0]).required("End date is required."),
  email: yup.string().default(""),
  fullname: yup.string().default(""),
  orderStatus: yup.string().default(""),
  eventName: yup.string().default(""),
  isDescending: yup.boolean().default(true),
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
  isDescending:boolean;
};

  
export default function OrderReport() {
  const history = useHistory();
  const  user = useAppSelector((state:RootState) => state.auth);
  const customerId = user.user?.customerId;
  const [recCount, setRecCount] = useState(0);
  const [filters, setFilters] = useState<FormValues | null>(null);

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

  const fetchOrders = async (data:FormValues, lastRowData:any) => {
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
    queryParams.append("isAscending", (!data.isDescending).toString());
    
    if (lastRowData != null)
    {
      const dateCursor = new Date(lastRowData.orderDate).toISOString();
      queryParams.append("dateCursor", dateCursor);
      queryParams.append("orderidCursor", lastRowData.orderId.toString());
    }
    const queryString = queryParams.toString();
    console.log("Generated query string:", queryString);
    try{
      const response =await axiosClient.get(`/SalesOrderByCustomer/${customerId}?${queryString}`);
      console.log('Order report data fetched successfully:', response.data);
      toast.success("Report data fetched.");
      //setOrders(response.data);
      console.log("Response data:", response.data);
      if (lastRowData == null)
        setRecCount(response.data.length);
      else
        setRecCount(prevCount => prevCount + response.data.length);

      return {orders:response.data};
      // Handle the response data as needed
    }
    catch(error) {
      console.error('Error fetching order report data:', error);
      toast.error("Error fetching report data");
      return {orders:[]};
      // Handle error (e.g., show notification to user)
    }
  }

  const {
    data: orderPages,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    ...rest
  } = useInfiniteQuery(
    ['orders', filters],
    async ({ pageParam = null}) => {
      if (!filters) return { orders: [] };
      const data = await fetchOrders(filters, pageParam);
      console.log("Fetched orders page:", data);
      return data; // expects { orders: [], hasMore: true/false }
    },
    {
      staleTime: 1000 * 60 * 5,  // Data stays fresh for 5 minutes
      cacheTime: 1000 * 60 * 30, // Cache persists for 30 minutes
      refetchOnMount: false,      // don’t always re-fetch on mount
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      enabled: !!filters,
      getNextPageParam: (lastPage, allPages) =>{
        if (lastPage.orders.length < pageSize) return undefined; 
        return lastPage.orders[lastPage.orders.length - 1];
      }
    }
  );

  const onSubmit = async (data: FormValues,errors:any) => {
    console.log("✅ Submitted data:", data);
    console.log("❌ Validation errors:", errors); 
    //await fetchOrders(data);
     setFilters(data);
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
        eventName: "",
        isDescending:true
      });
      // if (orders.length>0)
      //   setOrders( orders);
    }, [events,reset]);
    
    if (isEventsLoading) return <p>Loading...</p>;
  return (
    <IonPage>
      <IonHeader>
        <AppNavbar />
      </IonHeader>
      <IonContent>
        <div className="flex flex-col  min-h-full">
       
          <form onSubmit={handleSubmit(onSubmit)}
            className="max-w-4xl w-full mx-auto p-3 border border-gray-300 rounded-lg shadow-lg bg-brand-neutral"
          >  
            <Toaster position="top-right" />
            <div className="text-center text-xl font-bold text-accent-color mb-6">Order Report</div>

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
                  <option value="InProgress">In Progress</option>
                  <option value="Reserved">Reserved</option>
                  <option value="Timedout">Timedout</option>
                  <option value="Replaced">Replaced</option>
                  <option value="Abandoned">Abandoned</option>
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
                  {events?.map((event:any) => (
                    <option key={event.eventId} value={event.eventId}>
                      {event.eventName}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {/* Submit Button */}
            <div className="flex mt-4 mb-4 justify-between items-center">
              <div className="flex items-center mr-auto">
                <label className="font-semibold self-center">Show latest orders on top</label>
                <input type="checkbox" id="includeDetails" className="ml-2 mt-1" {...register("isDescending")}/>
              </div>
              {recCount > 0 && (
                  <Button  className="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700 mr-4"
                    onClick= {async (e)=>{
                      e.preventDefault();
                      
                      // const url =`${API_BASE_URL}/DownloadOrderReport/${customerId}?${filters ? new URLSearchParams({
                      // startDate: filters.startDate,
                      // endDate: filters.endDate,
                      // emailAddress: filters.email || '',
                      // name: filters.fullname || '',
                      // orderStatus: filters.orderStatus || '',
                      // eventId: filters.eventName == ''? '0': filters.eventName,
                      // isAscending: (!filters.isDescending).toString()
                      // }).toString() : ""}`;
                      // console.log("Downloading report from",url);
                      
                      // window.location.href = url;
                      if (filters)
                      {
                        const queryParams = new URLSearchParams({
                          startDate: filters.startDate,
                          endDate: filters.endDate,
                          emailAddress: filters.email || '',
                          name: filters.fullname || '',
                          orderStatus: filters.orderStatus || '',
                          eventId: filters.eventName === '' ? '0' : filters.eventName,
                          isAscending: (!filters.isDescending).toString()
                        });
                        try {
                          const response = await axiosClient.get(
                            `/DownloadOrderReport/${customerId}?${queryParams.toString()}`,
                            { responseType: 'blob' }
                          );
                          console.log("Report downloaded successfully:", response);
                          const url = window.URL.createObjectURL(new Blob([response.data]));
                          console.log("Generated download URL:", url);
                          const link = document.createElement('a');
                          link.href = url;
                          link.setAttribute('download', 'order-report.csv');
                          document.body.appendChild(link);
                          link.click();
                          link.parentNode?.removeChild(link);
                        } catch (error) {
                          toast.error('Failed to download report');
                        }

                      }


                    }
                    }>
                    Download CSV
                  </Button>
                // </a>
              )}
              

              <label className="mr-4 font-semibold">Total Records: {recCount}</label>
              <button
                type="submit"
                className="bg-brand-dark text-white text-brand-neutral px-4 py-2 rounded hover:bg-blue-700"
              >
                Search
              </button>      
            </div>
            {orderPages && orderPages.pages.length > 0 &&(
                <div className="border-l-2 pl-2">
                <table className="table-auto w-full mt-4">
                  <thead>
                    <tr>
                      <th >Date</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Event</th>
                      <th>Status</th>
                      <th >Total</th>
                      <th>Count</th>
                    </tr>
                  </thead>
                  <tbody>
                    {orderPages.pages.map((page, i) => (
                    <React.Fragment key={i}>
                      {console.log("Rendering page", i, page,page.orders)}
                      {page.orders.map((row:any) => (
                        <tr key={row.orderId} className="hover:bg-gray-100 border-b text-center">
                          <td className="max-w-[4rem] truncate overflow-hidden whitespace-nowrap" title={row.orderDate}>{new Date(row.orderDate).toLocaleDateString()}</td>
                          <td className="max-w-[8rem] truncate overflow-hidden whitespace-nowrap" title={row.fullName}>{row.fullName}</td>
                        
                          <td className="max-w-[8rem] truncate overflow-hidden whitespace-nowrap" title={row.emailAddress}>
                          {row.emailAddress}
                          </td>
                          <td className="max-w-xs whitespace-normal break-words" title={row.eventName}>{row.eventName}</td>
                          <td className="max-w-[4rem] truncate overflow-hidden whitespace-nowrap" title={row.salesOrderStatus}>{row.salesOrderStatus}</td>
                      
                          <td>
                            ${row.orderTotal.toFixed(2)}
                          </td>
                          <td>{row.orderCount}</td>
                        </tr>
                        ))}
                    </React.Fragment>
                    ))}
                  
                  </tbody>
                  </table>
                  {hasNextPage && (
                      <button onClick={() => fetchNextPage()} disabled={isFetchingNextPage}>
                        {isFetchingNextPage ? 'Loading more...' : 'Load More'}
                      </button>
                    )}

                </div>
              )}
            
          </form>
          <Footer/>
        </div>
    </IonContent>
    </IonPage>
      
   
  );
}
