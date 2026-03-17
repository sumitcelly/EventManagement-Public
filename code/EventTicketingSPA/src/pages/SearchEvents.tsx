import { useInfiniteQuery, useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { Link } from "react-router-dom";
import { EventCard } from "../components/Card";
import { AppPagination } from "../components/Pagination";
import App from "../App";
import { useEffect, useState } from "react";
import{useParams} from "react-router";
import { get } from "react-hook-form";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../components/Navbarnew";
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
  eventBannerUrl: string;
  organizerUrlName:string;
  eventUrlName:string;
}

console.log("SearchEvents rendered");

const pageSize =5;
export default function EventsPage() {
   
    const { keyword: paramKeyword, location: paramLocation } = useParams<{ keyword?: string; location?: string }>();
    const keyword = paramKeyword ?? "";
    const location = paramLocation ?? "";
    let state = "";
    let city="";
    console.log('location and keyword',location,keyword);
    if (location.length == 2)
      state = location;
    if (location.includes(',')) {
      city =  location.split(',')[0].trim();
      state = location.split(',')[1].trim();
    }

    console.log('city and state',city,state);

    const fetchEvents = async ({ pageParam = null }) => {
      const res = await axiosClient.get("/events/search", {
        params: {
          keyword,
          city: city,
          state: state,
          limit: pageSize,
          cursor: pageParam, // null on first load, lastEventDate on subsequent
        },
      });
        console.log("SearchEvents completed res", res.data);
        if (res.data && res.data.length > 0)
        {
          // res.data.forEach((e: EventSearchResult) => 
          // {
          //   e.eventImageUrl = "/images/concert.jpg";
          // });
        }
      return res.data;
    };

    const {
        data,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
        isLoading
      } = useInfiniteQuery(["events", keyword, location], fetchEvents, {
        getNextPageParam: (lastPage) => {
        if (lastPage.length < pageSize) return undefined; // no more results
       
        return lastPage[lastPage.length - 1].eventDate; // 👈 use cursor
      },staleTime: 1000 * 60 * 5
    });
    //const { data, isLoading } = useQuery(["searchevents", keyword, location, currentPage], getData,  { staleTime: 1000 * 60 });
    
    // useEffect(() => {
    // if (data && data.length > 0 && currentPage === 1) {
    //   setTotalItems(data.length);
    // }
    // }, [data, currentPage]);
    
    if (isLoading) return <p>Loading...</p>;

  return (
    
        <IonPage>
          <IonHeader><AppNavbar/></IonHeader>
         <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
    <h4 className="text-xl font-bold m-2 flex justify-center">Events you maybe interested in</h4>
    <div className="grid grid-cols-1 m-6 sm:grid-cols-2 md:grid-cols-5 gap-3 justify-items-center">
        {data?.pages.map((page) =>
           page.map((event:EventSearchResult) => <EventCard key={event.eventId} event={event} />)
        )}
    </div>
     {/* <AppPagination  totalItems={totalItems} currentPage={currentPage} itemsPerPage={8} onPageChange={onPageChange}/> */}
      <button className="mb-3  ml-4 bg-brand-dark text-white px-4 
                        py-2 rounded hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          onClick={() => fetchNextPage()}
          disabled={!hasNextPage || isFetchingNextPage}>
          {isFetchingNextPage ? "Loading..." : hasNextPage ? "Load More" : "No More Results"}
      </button>

   </IonContent>
       </IonPage>
  );
}
