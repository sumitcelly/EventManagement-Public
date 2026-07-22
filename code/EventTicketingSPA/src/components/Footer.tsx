import { Link } from 'react-router-dom';
import { useAppSelector } from '../app/hook';
import { useQuery } from 'react-query';
import axiosClient from '../api/axiosClient';
import { useCompanyDetails } from '../utils/CompanyQuery';

export default function Footer() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const role = user?.role || "";
  const getSellTicketLink = () => {
    if (!isAuthenticated) {
      return "/login?ref=selltickets";
    }
    if (isAuthenticated && (role === "Owner" || role === "FullAdmin" || role === "RestrictedAdmin")) {
      return "/Dashboard";
    }
    if (isAuthenticated && role === "Attendee") {
      return "/OrganizerManager";
    }
    return "/";
  };

  const {data,isLoading, error} = useCompanyDetails();
  

  if (isLoading) 
    return (<p>Loading...</p>);
  
  return (
    <footer className="border-t  bg-brand-neutral px-4 py-3 sm:px-6 dark:bg-gray-900 mt-auto w-full">
      <div className="w-full max-w-screen-xl mx-auto">
        <div className="grid w-full justify-between sm:flex sm:justify-between md:grid-cols-3">
          <div className="mb-3 md:mb-0">
            <h2 className="mb-2 text-lg font-bold text-gray-900 dark:text-white">{data.platformName}</h2>
            <p className="mt-2 max-w-xs text-gray-500 text-xs dark:text-gray-400">
              The most reliable way to discover and book tickets for local events.
            </p>
          </div>
          <div className="grid grid-cols-2 gap-8 sm:gap-6 md:grid-cols-3">
            <div>
              <h2 className="mb-2 text-xs font-semibold text-gray-900 uppercase dark:text-white">
                Ticketing
              </h2>
              <ul className="text-gray-700 dark:text-gray-400 font-medium text-xs">
                <li className="mb-2">
                  <Link to={getSellTicketLink()} className="hover:underline">
                    Organize Events
                  </Link>
                </li>
                <li>
                  <Link to="/pricing" className="hover:underline">
                    Pricing
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h2 className="mb-2 text-xs font-semibold text-gray-900 uppercase dark:text-white">
                Support
              </h2>
              <ul className="text-gray-700 dark:text-gray-400 font-medium text-xs">
                <li className="mb-2">
                  <Link to="/helpcenter" className="hover:underline">
                    Help Center & FAQ
                  </Link>
                </li>
                <li className="mb-2">
                  <a href={`mailto:${data.supportEmail}`} className="hover:underline">
                    Contact Us
                  </a>
                </li>
                
              </ul>
            </div>
            <div>
              <h2 className="mb-2 text-xs font-semibold text-gray-900 uppercase dark:text-white">
                Legal
              </h2>
              <ul className="text-gray-700 dark:text-gray-400 font-medium text-xs">
                <li className="mb-2">
                  <Link to="/privacypolicy" className="hover:underline">
                    Privacy Policy
                  </Link>
                </li>
                <li className="mb-2">
                  <Link to="/tos" className="hover:underline">
                    Terms of Service
                  </Link>
                </li>
                <li>
                  <Link to="/refundpolicy" className="hover:underline">
                    Refund Policy
                  </Link>
                </li>
              </ul>
            </div>
          </div>
        </div>
        <hr className="my-3 border-gray-200 sm:mx-auto dark:border-gray-700" />
        <div className="w-full sm:flex sm:items-center sm:justify-between">
          <span className="text-xs text-gray-500 sm:text-center dark:text-gray-400">
            <Link to="#" className="hover:underline">{data.registeredCompanyName}</Link>. All Rights Reserved.
          </span>
        </div>
      </div>
    </footer>
  );
}
