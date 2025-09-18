
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
import { useAppDispatch ,useAppSelector} from "../app/hook";
import { logout } from "../features/auth/authSlice";
import axios from "axios";
import { Link } from "react-router-dom";
import { useState } from "react";


export function AppNavbar() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const [showSearch, setShowSearch] = useState(false);

  const handleLogout = async () => {
    await axios.post(
      "http://localhost:5220/user/logout",
      {},
      { withCredentials: true }
    );
    dispatch(logout());
  };

  return (
    <Navbar fluid rounded className="bg-brand-light m-1 mb-3 shadow-md">
      <NavbarBrand href="https://flowbite-react.com">
        <img src="/vite.svg" className="mr-3 h-6 sm:h-9" alt="Flowbite React Logo" />
        <span className="self-center whitespace-nowrap text-xl font-semibold dark:text-white">EventsNow</span>
      </NavbarBrand>
      {/* for screens mediume and lrger, shows up at the end on right  */}
      <div className="flex md:order-2">
        {isAuthenticated && user?.email
            ?(<>
            <Dropdown
            arrowIcon={false}
            inline
            label={
                <Avatar alt="User settings" img="https://flowbite.com/docs/images/people/profile-picture-4.jpg" rounded />
            }>
                <DropdownHeader>
                    <span className="block text-sm">{user?.email}</span>
                    {/* <span className="block truncate text-sm font-medium">name@flowbite.com</span> */}
                </DropdownHeader>
                <DropdownItem>My Orders</DropdownItem>
                <DropdownDivider />
                <DropdownItem onClick={handleLogout}>Sign out</DropdownItem>
            </Dropdown>
            </>)  
            :(<><button className="bg-brand text-white px-4 py-2 rounded-lg mx-3"
                onClick={() => {window.location.href = "/login";}}>
                Login
                </button>
                {/* <NavbarCollapse>
                <NavbarLink href="/login" className="hover:underline">Login</NavbarLink>
                </NavbarCollapse> */}
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
    {showSearch && (
        <div className="md:hidden mt-4 flex justify-center w-full">
          <div className="w-2/3 max-w-xl px-4">
            {/* full-width search input */}
            <form onSubmit={(e) => e.preventDefault()} className="relative">
            <div className="flex flex-col justify-center">
              <div className="flex gap-1">
                <input
                  type="text"
                  id="search-navbar-mobile"
                  className="w-full p-2 mb-3 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Keyword..."
                />
                <button              
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
                id="location-navbar-mobile"
                className="w-4/5 flex-1 p-2 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Location..."
              />
          </div>
          </form>
          </div>
        </div>
      )}
      {/*hidden on small screens, visible on medium and larger*/}
      <div className="relative hidden md:block ml-auto">
        
          <div className="flex space-x-2">
            <input type="text" id="search-navbar" 
              className="block w-full p-2 ps-10 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" 
              placeholder="Keyword..."/>
            <input type="text" id="location-navbar" 
              className="block w-full p-2 ps-10 text-sm text-gray-900 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" 
              placeholder="Location..."/>
            <button type="submit" className="p-2.5 text-sm font-medium text-white bg-blue-700 rounded-lg border border-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">
              <svg className="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                  <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
              </svg>
              <span className="sr-only">Search</span>
            </button>
    
         </div>
        {/*no idea what this button does, it is hidden*/}
          {/* <button type="button" className="md:hidden inline-flex items-center p-2 w-10 h-10 justify-center text-sm text-gray-500 rounded-lg hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-gray-200 dark:text-gray-400 dark:hover:bg-gray-700 dark:focus:ring-gray-600" aria-controls="navbar-search" aria-expanded="false">
            <span className="sr-only">Open main menu</span>
            <svg className="w-5 h-5" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 17 14">
                <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M1 1h15M1 7h15M1 13h15"/>
            </svg>
        </button>  */}
      </div>

      <NavbarCollapse className="ml-auto mr-5">
        <NavbarLink href="#" active>
          Organize an Event
        </NavbarLink>
        <NavbarLink href="#">Find my tickets</NavbarLink>
        <NavbarLink href="#">Help</NavbarLink>
      </NavbarCollapse>
    </Navbar>
  );
}
export default AppNavbar;