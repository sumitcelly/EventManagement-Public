import { BsFacebook, BsInstagram, BsTwitter, BsGithub } from 'react-icons/bs';

export default function TicketFooter() {
  return (
    <footer className="border-t  bg-brand-neutral px-4 py-3 sm:px-6 dark:bg-gray-900 mt-auto w-full">
      <div className="w-full max-w-screen-xl mx-auto">
        <div className="grid w-full justify-between sm:flex sm:justify-between md:grid-cols-3">
          <div className="mb-3 md:mb-0">
            <h2 className="mb-2 text-lg font-bold text-gray-900 dark:text-white">TicketPro</h2>
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
                  <a href="#" className="hover:underline">
                    Find Events
                  </a>
                </li>
                <li className="mb-2">
                  <a href="#" className="hover:underline">
                    Sell Tickets
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:underline">
                    Pricing
                  </a>
                </li>
              </ul>
            </div>
            <div>
              <h2 className="mb-2 text-xs font-semibold text-gray-900 uppercase dark:text-white">
                Support
              </h2>
              <ul className="text-gray-700 dark:text-gray-400 font-medium text-xs">
                <li className="mb-2">
                  <a href="#" className="hover:underline">
                    Help Center
                  </a>
                </li>
                <li className="mb-2">
                  <a href="#" className="hover:underline">
                    Contact Us
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:underline">
                    FAQs
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
                  <a href="#" className="hover:underline">
                    Privacy Policy
                  </a>
                </li>
                <li className="mb-2">
                  <a href="#" className="hover:underline">
                    Terms of Service
                  </a>
                </li>
                <li>
                  <a href="#" className="hover:underline">
                    Refund Policy
                  </a>
                </li>
              </ul>
            </div>
          </div>
        </div>
        <hr className="my-3 border-gray-200 sm:mx-auto dark:border-gray-700" />
        <div className="w-full sm:flex sm:items-center sm:justify-between">
          <span className="text-xs text-gray-500 sm:text-center dark:text-gray-400">
            © 2024 <a href="#" className="hover:underline">TicketPro™</a>. All Rights Reserved.
          </span>
          <div className="flex mt-2 space-x-4 sm:justify-center sm:mt-0">
            <a href="#" className="text-gray-500 hover:text-gray-900 dark:hover:text-white">
              <BsFacebook className="w-4 h-4" />
            </a>
            <a href="#" className="text-gray-500 hover:text-gray-900 dark:hover:text-white">
              <BsInstagram className="w-4 h-4" />
            </a>
            <a href="#" className="text-gray-500 hover:text-gray-900 dark:hover:text-white">
              <BsTwitter className="w-4 h-4" />
            </a>
            <a href="#" className="text-gray-500 hover:text-gray-900 dark:hover:text-white">
              <BsGithub className="w-4 h-4" />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}
