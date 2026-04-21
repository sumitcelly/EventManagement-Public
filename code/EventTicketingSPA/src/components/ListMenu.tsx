import { Dropdown,DropdownItem } from "flowbite-react";
import { HiOutlineDotsVertical } from "react-icons/hi";
import {  useHistory } from "react-router-dom";
import { YesNoModal } from "./YesNoModal"; 
import { useState } from "react";
import { useIonRouter } from "@ionic/react";

export interface ListMenuData{
    viewLink:string,
    editLink:string,
    editData?:any
    delete:()=>void,
    previewData?:()=>void
}

export default function  ListMenu({linkData}:{linkData:ListMenuData}) {
  const [openModal, setOpenModal] = useState(false);
  const history = useHistory();
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
        {linkData.viewLink && (
          <DropdownItem onClick={() => history.push(linkData.viewLink)}>
            View Details
          </DropdownItem>
        )}
        {linkData.editLink && (
        <DropdownItem onClick={() => history.push(linkData.editLink, linkData?.editData)}>
          Edit
        </DropdownItem>
        )}
        
        {linkData.previewData && (
          <DropdownItem onClick={() => linkData?.previewData && linkData.previewData()}>
            Preview
          </DropdownItem>
        )}
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
