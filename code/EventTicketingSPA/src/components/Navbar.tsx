
import {
  Avatar,
  Dropdown,
  DropdownDivider,
  DropdownHeader,
  DropdownItem,
  Navbar,
  NavbarBrand,
  NavbarCollapse,
  NavbarLink,
  NavbarToggle
} from "flowbite-react";
import { useIonRouter } from '@ionic/react';

import { useAppDispatch ,useAppSelector} from "../app/hook";
import { logout } from "../features/auth/authSlice";
import axios from "axios";
import { useState } from "react";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useForm } from "react-hook-form";
import axiosClient from "../api/axiosClient";
import { useHistory } from "react-router";

interface SearchFormInputs {
  keyword: string ;
  location: string ;
}

const schema = yup
  .object({
    keyword: yup.string().default(""),
    location: yup.string().default(""),
  })
 .test(
  "at-least-one",      // name
  "Please enter at least a keyword or a location", // default error message
  (items, ctx) => {
    if (!items?.keyword && !items?.location) {
      return ctx.createError({
        path: "keyword", // attach error to keyword
        message: "Please enter at least a keyword or a location",
      });
    }
    return true;
  }
);
    

export function AppNavbar() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const role = user?.role || "";
  
  const dispatch = useAppDispatch();
  const [showSearch, setShowSearch] = useState(false);
 // const navigate = useNavigate();
  const history = useHistory();

  const handleLogout = async () => {
    await axiosClient.post(
      "/user/logout",
      {},
      { withCredentials: true }
    );
    dispatch(logout());
    history.replace("/login");
  }

    const {
        register,
        handleSubmit,
        formState: { errors }
      } = useForm<SearchFormInputs>({
        resolver: yupResolver(schema),
        defaultValues: { keyword: "", location: "" },
      });


  const onSubmit = (data: any) => {

      console.log('insubmit',data,data.location,data.keyword);
      console.log("form errors", errors);
      let queryParams = "";
        if (!data.keyword && !data.location) return;
      if (data.location) {
        queryParams += `location/${encodeURIComponent(data.location)}`;
      }
      if (data.keyword) {
        if (queryParams) queryParams += "/";
        queryParams += `keyword/${encodeURIComponent(data.keyword)}`;
      }
      console.log(`/searchevents/${queryParams}`);
      //window.location.href =`/searchevents/${queryParams}`;
      history.push(`/searchevents/${queryParams}`);
    };

  const checkScannerAccess = () => {
    return isAuthenticated && role !== "Attendee";
  }

   const checkBasicAdminAccess = () => {
    return isAuthenticated && (role === "Owner" || role === "FullAdmin" || role === "RestrictedAdmin");
  }

  return (
    <Navbar fluid rounded className="bg-brand-light m-1 mb-3 shadow-md">
      <NavbarBrand href="https://flowbite-react.com">
        <img src="/vite.svg" className="mr-3 h-6 sm:h-9" alt="Flowbite React Logo" />
        <span className="self-center whitespace-nowrap text-xl font-semibold dark:text-white">EventsNow</span>
      </NavbarBrand>
      {/* for screens mediume and lrger, shows up at the end on right  */}
      <div className="flex md:order-2">
        {isAuthenticated && user?.email && !user?.guest
            ?(<>
              <Dropdown
              arrowIcon={true}
              className="mr-1"
              // inline={true}
              label={user.name || `Welcome`}
                  // <Avatar alt="User settings"
                  //     img="https://flowbite.com/docs/images/people/profile-picture-1.jpg" rounded />
                  // }
                >
                <DropdownHeader>
                    <span className="block text-sm">{user?.email}</span>
                </DropdownHeader>
                {checkScannerAccess() && <DropdownItem href="/scannerdashboard">Scan Tickets</DropdownItem>}
                <DropdownItem href='/myevents'>Find my tickets</DropdownItem>
                <DropdownDivider />
                <DropdownItem onClick={handleLogout}>Sign out</DropdownItem>
            </Dropdown>
            </>)  
            :(<><button className="bg-brand text-white px-4 py-2 rounded-lg mx-3"
                onClick={() => {window.location.href = "/login";}}>
                Login
                </button>
                </>)}
        <NavbarToggle />
      </div>
      {/* Search Bar code */}

      {/*visible on small screens, hidden on medium and larger*/}
      <button type="button" aria-controls="navbar-search" aria-expanded="false" id="mobile-search-button"
          onClick={() => setShowSearch(!showSearch)}
          className="md:hidden text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-700 rounded-lg text-sm p-2.5 me-1">
          <svg className="w-5 h-5" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
              <path stroke="currentColor"strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" 
              d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
          </svg>
          <span className="sr-only">Search</span>
      </button>

      {/*hidden on small screens, visible on medium and larger*/}
      
    <form onSubmit={ handleSubmit(onSubmit)} className="relative w-full md:ml-auto md:w-1/3" >
      {/*desktop version*/}
      <div className="relative hidden md:block ml-auto">
        <div className="flex space-x-2">
          <input type="text" id="search-navbar" 
          
          {...register("keyword")}
            className="block w-full p-2 ps-10 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" 
            placeholder="Keyword..."
            />
          <input type="text" id="location-navbar" 
          
          {...register("location")}
            className="block w-full p-2 ps-10 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" 
            placeholder="Location..."/>
          <button type="submit"  
          className="p-2.5 text-sm font-medium text-white bg-blue-700 rounded-lg border border-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">
            <svg className="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
            </svg>
            <span className="sr-only">Search</span>
          </button>       
        </div>           
        {errors.keyword?.message && <p>{errors.keyword.message}</p>}
      </div>
      {/*desktop version ends*/}
      {/*mobile version*/}
      {showSearch && 
      (         
        <div className="md:hidden flex flex-col items-center mt-4">
        <div className="w-2/3">
              <div className="flex gap-1">
                <input
                  type="text"
                  {...register("keyword")}
                  id="search-navbar-mobile"
                  className="w-full p-2 mb-3 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Keyword..."
                />
                <button              
                  type="submit"
                  className="w-1/5 h-4/5 p-2.5 text-sm text-white bg-blue-700 rounded-lg hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300"
                    aria-label="Search">
                    <svg className="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                      <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
                    </svg>
                  </button>
              </div>
              {/* location + search button aligned on one row */}
          
              <input
                type="text"
                {...register("location")}
                id="location-navbar-mobile"
                className="w-4/5 p-2 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Location..."
              />
              {errors?.keyword && <p>{errors.keyword.message}</p>}                  
          </div>
          </div>
      )}
      </form>

      <NavbarCollapse className="ml-auto mr-5">
        {checkBasicAdminAccess() &&
        (
             <Dropdown
              arrowIcon={true}          
              label={`Organizer Menu`}>
                <DropdownItem href="/dashboard" >Organize an event</DropdownItem>
                <DropdownItem href="/organizermanager" >Organizer Info</DropdownItem>
                <DropdownItem href="/emailcampaigns" >Email Campaigns</DropdownItem>
                <DropdownItem href="/Organizer/SalesOrderReport" >Sales report</DropdownItem>
                <DropdownItem href="/teammanager" >Manage teams</DropdownItem>            
            </Dropdown>
        )}
  
        {(!isAuthenticated || user?.guest) &&(
          <>
            <NavbarLink href="/auth/sendsecurecode/myevents">Find my tickets</NavbarLink>
            <NavbarLink href="/auth/sendsecurecode/signup">Signup</NavbarLink>
          </>
        )}
      </NavbarCollapse>
    </Navbar>
  );
}
export default AppNavbar;