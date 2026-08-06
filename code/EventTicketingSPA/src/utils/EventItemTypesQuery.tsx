import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";


const fetchEventItemTypes = async (eventId:number|string|undefined) => {

  const res = await axiosClient.get(`/eventitemtype/all/${eventId}`);
  console.log('Event item types details', res?.data);
  return res.data;
};

export function useEventItemTypes(eventId:number|string|undefined) {
  const { data:eventItemTypeData, isLoading:eventItemTypesLoading, error:eventItemTypesError } = useQuery(
    ["TicketsbyEvent", eventId],
    () => fetchEventItemTypes(eventId),
    {
      staleTime: 1000 * 60 * 60,
      cacheTime: 1000 * 60 * 60,
      refetchOnWindowFocus: false,
    }
  );
  return { eventItemTypeData, eventItemTypesLoading, eventItemTypesError };
}


