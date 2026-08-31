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

const pageSize =20;
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
  const [filtersOpen, setFiltersOpen] = useState(true);

  const { data:events, isLoading:isEventsLoading } = 
  useQuery(['EventsByCustomerId',customerId], async () => {
     console.log("Fetching events for customer", customerId);
     try
     {
      const res = await axiosClient.get(`/Events/ByCustomer/${customerId}/true/true`);
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
      const response =await axiosClient.get(`/SalesOrder/Customer/${customerId}?${queryString}`);
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
        <div className="flex flex-col min-h-full bg-gray-50">
          <div className="w-full max-w-[1600px] mx-auto px-3 py-3">
            <div className="flex flex-col xl:flex-row xl:items-start gap-3">
              <form onSubmit={handleSubmit(onSubmit)} className="w-full xl:max-w-[330px] xl:flex-shrink-0">
                <Toaster position="top-right" />
                <div className="p-3 border border-gray-300 rounded-md shadow-sm bg-brand-neutral">
                  <div className="flex items-center justify-between gap-2 mb-3">
                    <div className="text-lg font-bold text-accent-color">Order Report</div>
                    <button
                      type="button"
                      onClick={() => setFiltersOpen((prev) => !prev)}
                      className="text-xs font-medium text-gray-700 border border-gray-300 rounded px-2 py-1 bg-gray-50 hover:bg-gray-100"
                    >
                      {filtersOpen ? "Hide" : "Show"}
                    </button>
                  </div>

                  {filtersOpen && (
                    <>
                      <div className="flex flex-col gap-2 mb-2">
                        <div className="grid grid-cols-2 gap-2">
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">Start Date</label>
                            <input
                              type="date"
                              {...register("startDate")}
                              className="w-full border rounded p-1.5 text-sm"
                            />
                            <div className="min-h-[18px]">
                              {errors.startDate && (
                                <p className="text-red-600 text-xs mt-1">{errors.startDate.message}</p>
                              )}
                            </div>
                          </div>
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">End Date</label>
                            <input
                              type="date"
                              {...register("endDate")}
                              className="w-full border rounded p-1.5 text-sm"
                            />
                            <div className="min-h-[18px]">
                              {errors.endDate && (
                                <p className="text-red-600 text-xs mt-1">{errors.endDate.message}</p>
                              )}
                            </div>
                          </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">Email</label>
                            <input
                              type="text"
                              {...register("email")}
                              className="w-full border rounded p-1.5 text-sm"
                            />
                            <div className="min-h-[18px]"></div>
                          </div>
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">Name</label>
                            <input
                              type="text"
                              {...register("fullname")}
                              className="w-full border rounded p-1.5 text-sm"
                            />
                            <div className="min-h-[18px]"></div>
                          </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">Order Status</label>
                            <select
                              {...register("orderStatus")}
                              className="w-full border rounded p-1.5 text-sm"
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
                          <div className="flex flex-col space-y-1">
                            <label className="text-sm font-semibold">Event Name</label>
                            <select
                              {...register("eventName")}
                              className="w-full border rounded p-1.5 text-sm"
                            >
                              <option value="">Select</option>
                              {events?.map((event:any) => (
                                <option key={event.eventId} value={event.eventId}>
                                  {event.eventName}
                                </option>
                              ))}
                            </select>
                          </div>
                        </div>
                      </div>

                      <div className="flex flex-wrap items-center justify-between gap-2 mt-3">
                        <div className="flex items-center text-sm">
                          <label className="font-medium">Newest first</label>
                          <input type="checkbox" id="includeDetails" className="ml-2 h-4 w-4" {...register("isDescending")}/>
                        </div>

                        <div className="ml-auto flex items-center gap-2 flex-wrap">
                          {recCount > 0 && (
                            <Button
                              className="bg-green-600 text-white px-3 py-2 rounded hover:bg-green-700 text-xs"
                              onClick={async (e) => {
                                e.preventDefault();
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
                                      `/SalesOrder/DownloadOrderReport/${customerId}?${queryParams.toString()}`,
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
                              }}
                            >
                              Download CSV
                            </Button>
                          )}

                          <label className="text-sm font-semibold whitespace-nowrap">Total: {recCount}</label>
                          <button
                            type="submit"
                            className="bg-brand-dark text-white px-3 py-2 rounded hover:bg-blue-700 text-sm"
                          >
                            Search
                          </button>
                        </div>
                      </div>
                    </>
                  )}

                  {!filtersOpen && (
                    <div className="flex items-center justify-between gap-2 text-sm text-gray-700">
                      <span className="font-medium">Filters hidden</span>
                      <span className="font-semibold">{recCount} total</span>
                    </div>
                  )}
                </div>
              </form>

              <div className="flex-1 min-w-0">
                {orderPages && orderPages.pages.length > 0 && (
                  <div className="w-full border border-gray-300 rounded-md shadow-sm bg-brand-neutral overflow-hidden">
                    <div className="overflow-x-auto">
                      <table className="table-auto w-full text-sm align-middle">
                        <thead className="bg-gray-50 align-middle">
                          <tr className="align-middle">
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Date</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Name</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Email</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Event</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Status</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Total</th>
                            <th className="px-2 py-2 text-left align-middle font-semibold whitespace-nowrap">Count</th>
                          </tr>
                        </thead>
                        <tbody>
                          {orderPages.pages.map((page, i) => (
                            <React.Fragment key={i}>
                              {console.log("Rendering page", i, page, page.orders)}
                              {page.orders.map((row:any) => (
                                <tr key={row.orderId} className="hover:bg-gray-100 border-b text-left align-middle">
                                  <td className="px-2 py-2 align-middle whitespace-nowrap" title={row.orderDate}>{new Date(row.orderDate).toLocaleDateString()}</td>
                                  <td className="px-2 py-2 align-middle max-w-[10rem] truncate" title={row.fullName}>{row.fullName}</td>
                                  <td className="px-2 py-2 align-middle max-w-[12rem] truncate" title={row.emailAddress}>{row.emailAddress}</td>
                                  <td className="px-2 py-2 align-middle max-w-[16rem] whitespace-normal break-words" title={row.eventName}>{row.eventName}</td>
                                  <td className="px-2 py-2 align-middle whitespace-nowrap" title={row.salesOrderStatus}>{row.salesOrderStatus}</td>
                                  <td className="px-2 py-2 align-middle whitespace-nowrap">${row.orderTotal.toFixed(2)}</td>
                                  <td className="px-2 py-2 align-middle whitespace-nowrap">{row.orderCount}</td>
                                </tr>
                              ))}
                            </React.Fragment>
                          ))}
                        </tbody>
                      </table>
                    </div>

                    {hasNextPage && (
                      <div className="p-3 border-t border-gray-200 bg-gray-50">
                        <button
                          onClick={() => fetchNextPage()}
                          disabled={isFetchingNextPage}
                          className="bg-brand-dark text-white px-3 py-2 rounded text-sm hover:bg-blue-700 disabled:opacity-60"
                        >
                          {isFetchingNextPage ? 'Loading more...' : 'Load More'}
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
          <Footer />
        </div>
      </IonContent>
    </IonPage>
  );
}
