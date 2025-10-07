import { Dropdown,DropdownItem } from "flowbite-react";
import { HiOutlineDotsVertical } from "react-icons/hi";
import {  useNavigate } from "react-router-dom";

export interface ListMenuData{
    viewLink:string,
    editLink:string
}

export default function ListMenu({linkData}:{linkData:ListMenuData}) {
    const navigate = useNavigate();

  return (
    
      <Dropdown
        
        inline
        label={<HiOutlineDotsVertical className="text-xl cursor-pointer" />}
      >
        <DropdownItem onClick={() => navigate(linkData.viewLink)}>
          View Details
        </DropdownItem>
        <DropdownItem onClick={() => navigate(linkData.editLink)}>
          Edit
        </DropdownItem>
        <DropdownItem onClick={() => console.log("Delete")}>
          Delete
        </DropdownItem>
      </Dropdown>
    
  );
}
