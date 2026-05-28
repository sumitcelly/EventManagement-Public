import { QueryFunctionContext, useInfiniteQuery, useQuery, useQueryClient } from "react-query";
import axiosClient from "../api/axiosClient";
import { Link, useLocation   } from "react-router-dom";
import { EventCard } from "../components/Card";
import { AppPagination } from "../components/Pagination";
import App from "../App";
import { useEffect, useMemo, useState } from "react";
import{useParams} from "react-router";
import { get } from "react-hook-form";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import AppNavbar from "../components/Navbar";
import Footer from "../components/Footer";

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
const fetchEvents = async ({ queryKey, pageParam }: QueryFunctionContext<[string, string, string, string]>) => {
  const [_key, keyword, city, state] = queryKey;
  
  const res = await axiosClient.get("/events/search", {
    params: {       
      keyword, 
      city, 
      state, 
      limit: pageSize, 
      cursor: pageParam 
    },
  });
  console.log("Fetched events page with cursor", pageParam, "and results", res.data);
  return res.data;
};
export default function SearchEvents() {
   
  const { search } = useLocation();

  // 2. Wrap URLSearchParams in useMemo to track changes to 'search'
  const params = useMemo(() => new URLSearchParams(search), [search]);

  // 3. Extract your data normally
  const eventlocation = params.get("location") || "";
  const keyword = params.get("keyword") || "";
    console.log('location and keyword',eventlocation,keyword);
    let state = "";
    let city="";
 
    if (eventlocation.length == 2)
      state = eventlocation;
    else if (eventlocation.includes(',')) {
      city =  eventlocation.split(',')[0].trim();
      state = eventlocation.split(',')[1].trim();
    }
    else
    {
      city = eventlocation;
    }
    console.log('keyword,city and state',keyword, city,state);
    




    const {
        data,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
        isLoading
      } = useInfiniteQuery(["events", keyword, city,state], fetchEvents, {
        getNextPageParam: (lastPage) => {
        if (lastPage.length < pageSize){
          console.log("No more pages to fetch");
          return undefined; // no more results
        } 
       
        console.log("Next page param (last event date)", lastPage[lastPage.length - 1].eventDate);
        return lastPage[lastPage.length - 1].eventDate; // 👈 use cursor
      },

       staleTime: 1000 * 60 * 5, // 5 minutes: Keep data "fresh" for 5 mins
       cacheTime : 1000 * 60 * 30,
    });
  
    
    if (isLoading) return <p>Loading...</p>;

    console.log("Rendering events page with data", data);

    
  return (
    
    <IonPage>
      <IonHeader><AppNavbar/></IonHeader>
      <IonContent>
        <div className="flex flex-col min-h-full">
          <div>
          <h4 className="text-xl font-bold m-2 flex justify-center">Events you maybe interested in</h4>
          <div className="grid grid-cols-1 m-6 sm:grid-cols-2 md:grid-cols-5 gap-3 justify-items-center">
              {data?.pages.map((page) =>
                page.map((event:EventSearchResult) => <EventCard key={event.eventId} event={event} />)
              )}
          </div>
          <div className="flex mb-6">
            <button className="mb-3  ml-4 bg-brand-dark text-white px-4 
                        py-2 rounded hover:bg-blue-700 disabled:opacity-50 
                        disabled:cursor-not-allowed"
                onClick={() => fetchNextPage()}
                disabled={!hasNextPage || isFetchingNextPage}>
                {isFetchingNextPage ? "Loading..." : hasNextPage ? "Load More" : "No More Results"}
            </button>
          </div>
        </div>
      <Footer/>
      </div>
    </IonContent> 
    
  </IonPage>
  );
}
