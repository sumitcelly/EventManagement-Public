import { Pagination } from "flowbite-react";
import { useState } from "react";

export function AppPagination({totalItems, currentPage, onPageChange, itemsPerPage = 10}: { onPageChange: (page:number)=>void,  totalItems: number, currentPage: number,  itemsPerPage?: number,}) {
//   const [currentPage, setCurrentPage] = useState(1);

//   const onPageChange = (page: number) => setCurrentPage(page);

  return (
    <div className="flex overflow-x-auto sm:justify-center">
      <Pagination layout="table" currentPage={currentPage} itemsPerPage={itemsPerPage} totalItems={totalItems} onPageChange={onPageChange} />
    </div>
  );
}
export default AppPagination;