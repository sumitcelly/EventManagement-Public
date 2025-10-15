import { Dropdown,DropdownItem } from "flowbite-react";
import { HiOutlineDotsVertical } from "react-icons/hi";
import {  useNavigate } from "react-router-dom";
import { YesNoModal } from "./YesNoModal"; 
import { useState } from "react";

export interface ListMenuData{
    viewLink:string,
    editLink:string,
    delete:()=>void
}

export default function  ListMenu({linkData}:{linkData:ListMenuData}) {
  const [openModal, setOpenModal] = useState(false);
  const navigate = useNavigate();
  const handleDelete = () => {
      // Handle the actual delete operation here
      linkData.delete();
      console.log('Delete confirmed');
      setOpenModal(false);
  };

  return (
    <>
      <Dropdown
        
        inline
        label={<HiOutlineDotsVertical className="text-xl cursor-pointer"/>}
      >
        <DropdownItem onClick={() => navigate(linkData.viewLink)}>
          View Details
        </DropdownItem>
        <DropdownItem onClick={() => navigate(linkData.editLink)}>
          Edit
        </DropdownItem>
        <DropdownItem onClick={() => setOpenModal(true)}>
          Delete
        </DropdownItem>
      </Dropdown>
      {openModal && (
              <YesNoModal 
                  modalText="Are you sure you want to delete this event?" 
                  openModal={openModal}
                  onClose={() =>  setOpenModal(false)}
                  onConfirm={handleDelete}
              />
      )}
    </>
  );
}
