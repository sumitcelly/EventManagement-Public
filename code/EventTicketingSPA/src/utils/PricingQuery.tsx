import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";


const fetchPricingDetails = async (eventId?: number) => {
  const id = eventId ?? 0;
  const res = await axiosClient.get(`/payment/transactionfees/${id}`);
  console.log('Event pricing details', res?.data);
  return res.data;
};

export function usePricingDetails(eventId?: number) {
  return useQuery(
    ["pricing", eventId ?? 0],
    () => fetchPricingDetails(eventId),
    {
      staleTime: 1000 * 60 * 60,
      cacheTime: 1000 * 60 * 60,
      refetchOnWindowFocus: false,
    }
  );
}

export { usePricingDetails as PricingDetails };

