import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";
import { Link } from "react-router-dom";
import { EventCard } from "../components/Card";
import { AppPagination } from "../components/Pagination";
import App from "../App";
import { useState } from "react";
import{useParams} from "react-router";
// 
export interface EventSearchResult {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventHeadline: string;
  eventSummary: string;
  eventOrganizer: number;
  eventLocation: string;
  eventImageUrl: string;
}


   
export default function EventsPage() {
    const [currentPage, setCurrentPage] = useState(1);
    const [totalItems, setTotalItems] = useState(0);
    const onPageChange = (page: number) =>
      setCurrentPage(page);
    const { location, keyword } = useParams();
    console.log("SearchEvents location, keyword", location, keyword);

    const { data, isLoading } = useQuery(`events/search/${keyword}/${location}/`+currentPage, async () => {
      const res = await axiosClient.get(`/events/search?keyword=${keyword}&location=${location}&page=${currentPage}&offset=8`);
      if (currentPage === 1)
        setTotalItems(11);
      const events: EventSearchResult[] = [];
      if (currentPage === 1)
      {
          events.push({ eventId: 1, eventName: "Food Festival", eventDate: new Date(), eventHeadline: "Gourmet Food Festival", 
          eventSummary: "Taste dishes from top chefs and local favorites.", eventOrganizer: 4, eventLocation: "New York", 
          eventImageUrl: "/images/concert.jpg" });
          events.push({ eventId: 2, eventName: "Music festival", eventDate: new Date(), eventHeadline: "Modern Music Fest", eventSummary: "Explore contemporary music from around the world.", eventOrganizer: 2, eventLocation: "New York",
          eventImageUrl: "/images/concert.jpg" });
          events.push({ eventId: 3, eventName: "Art Exhibition", eventDate: new Date(), eventHeadline: "Modern Art Exhibition", eventSummary: "Explore contemporary artworks from around the world.", eventOrganizer: 2, eventLocation: "New York",
            eventImageUrl: "/images/concert.jpg" });
        
          events.push({ eventId: 4, eventName: "Tech Conference", eventDate: new Date(), eventHeadline: "Annual Tech Conference", eventSummary: "Join industry leaders to discuss the latest in technology.", eventOrganizer: 3, eventLocation: "New York", 
            eventImageUrl: "/images/concert.jpg" });
          events.push({ eventId: 5, eventName: "Marathon", eventDate: new Date(), eventHeadline: "City Marathon", eventSummary: "Participate in the annual city marathon and promote fitness.", eventOrganizer: 5, eventLocation: "New York", 
            eventImageUrl: "/images/concert.jpg"});
           events.push({ eventId: 6, eventName: "Book Fair", eventDate: new Date(), eventHeadline: "International Book Fair", eventSummary: "Discover new authors and attend book signings.", eventOrganizer: 6, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
    
       events.push({ eventId: 7, eventName: "Film Festival", eventDate: new Date(), eventHeadline: "International Film Festival", 
          eventSummary: "Watch premieres and meet filmmakers from around the globe.", eventOrganizer: 7, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
       events.push({ eventId: 8, eventName: "Theater Play", eventDate: new Date(), eventHeadline: "Broadway Theater Play", 
          eventSummary: "Experience a captivating performance by renowned actors.", eventOrganizer: 8, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
      }
      else if (currentPage === 2) {

      
       events.push({ eventId: 9, eventName: "Comedy Show", eventDate: new Date(), eventHeadline: "Stand-Up Comedy Night", 
          eventSummary: "Laugh out loud with top comedians in a fun-filled evening.", eventOrganizer: 9, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
        events.push({ eventId: 10, eventName: "Charity Gala", eventDate: new Date(), eventHeadline: "Annual Charity Gala", 
          eventSummary: "Support a good cause while enjoying an elegant evening.", eventOrganizer: 10, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
                events.push({ eventId: 11, eventName: "Science Expo", eventDate: new Date(), eventHeadline: "National Science Expo", 
          eventSummary: "Explore the latest scientific discoveries and innovations.", eventOrganizer: 11, eventLocation: "New York", 
        eventImageUrl: "/images/concert.jpg" });
      }
      else if (currentPage === 3)
      {
    
     

      }
      return events;
  });

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
