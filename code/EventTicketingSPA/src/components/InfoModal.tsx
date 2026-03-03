
import { Button, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { HiOutlineExclamationCircle } from "react-icons/hi";

export function InfoModal({modalText,openModal,onClose}:{modalText:string, openModal:boolean, onClose:()=>void}) {
    
  return (
    console.log("Rendering Info modal with openModal:", openModal),
      <Modal show={openModal} size="md" onClose={() => onClose()}  popup>
        <ModalHeader />
        <ModalBody>
          <div className="text-center">
            <HiOutlineExclamationCircle className="mx-auto mb-4 h-14 w-14 text-gray-400 dark:text-gray-200" />
            <h3 className="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">
              {modalText}
            </h3>
            <div className="flex justify-center gap-4">
              <Button color="red" onClick={() => onClose()}>
               Dismiss
              </Button>
              
            </div>
          </div>
        </ModalBody>
      </Modal>
   
  );
}
