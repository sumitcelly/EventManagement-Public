import { Pagination } from "flowbite-react";
import { useState } from "react";

export function AppPagination({totalItems, currentPage, onPageChange, itemsPerPage = 10}: { onPageChange: (page:number)=>void,  totalItems: number, currentPage: number,  itemsPerPage?: number,}) {

  return (
    <div className="flex mb-4 ml-4 overflow-x-auto sm:justify-center">
      <div className="[&_*]:text-primary-color [&_*]:dark:text-primary-color [&_span]:text-primary-color [&_span]:dark:text-primary-color">
        <Pagination
          layout="table"
          currentPage={currentPage}
          itemsPerPage={itemsPerPage}
          totalItems={totalItems}
          onPageChange={onPageChange}
        />
      </div>
    </div>
  );
}
export default AppPagination;