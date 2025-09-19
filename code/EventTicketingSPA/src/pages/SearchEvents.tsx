import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { Link } from "react-router-dom";
import { EventCard } from "../components/Card";
import { AppPagination } from "../components/Pagination";
import App from "../App";
import { useEffect, useState } from "react";
import{useParams} from "react-router";
import { get } from "react-hook-form";
// 
export interface EventSearchResult {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventHeadline: string;
  eventSummary: string;
  eventOrganizer: string;
  eventOrganizerId: number;
  eventLocation: string;
  free: boolean;
  eventImageUrl: string;
}


console.log("SearchEvents rendered");

export default function EventsPage() {
    const [currentPage, setCurrentPage] = useState(1);
    const [totalItems, setTotalItems] = useState(0);
    const onPageChange = (page: number) =>
      setCurrentPage(page);

    console.log("SearchEvents currentPage", currentPage);
    const { keyword: paramKeyword, location: paramLocation } = useParams();
    const keyword = paramKeyword ?? "";
    const location = paramLocation ?? "";
    console.log('locaion and keyword',location,keyword);

    const  getData=  async () => {
      const res = await axiosClient.get(`/events/search?keyword=${keyword}&city=${location}&page=${currentPage}&offset=0`);
      console.log("SearchEvents res", res);
    
      if (res.data && res.data.length > 0)
      {
        res.data.forEach((e: EventSearchResult) => 
        {
          e.eventImageUrl = "/images/concert.jpg";
        });
      }
      return res.data;
    };
    const { data, isLoading } = useQuery(["searchevents", keyword, location, currentPage], getData,  { staleTime: 1000 * 60 });
    
    useEffect(() => {
    if (data && data.length > 0 && currentPage === 1) {
      setTotalItems(data.length);
    }
    }, [data, currentPage]);
    
    if (isLoading) return <p>Loading...</p>;

  return (
    <>
    <h4 className="text-xl font-bold m-2 flex justify-center">Events you maybe interested in</h4>
    <div className="grid grid-cols-1 m-6 sm:grid-cols-2 md:grid-cols-5 gap-3 justify-items-center">
        {data && data.map((e:EventSearchResult) => (
          <EventCard key ={e.eventId} event={e}/>
        ))}
    </div>
    <AppPagination  totalItems={totalItems} currentPage={currentPage} itemsPerPage={8} onPageChange={onPageChange}/>
    </>
  );
}
