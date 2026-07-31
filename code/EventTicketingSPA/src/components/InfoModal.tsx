
import { Button, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { HiOutlineExclamationCircle } from "react-icons/hi";

export function InfoModal({modalText,openModal,onClose}:{modalText:string, openModal:boolean, onClose:()=>void}) {
    
  return (
    console.log("Rendering Info modal with openModal:", openModal),
      <Modal show={openModal} size="md" onClose={() => onClose()}  popup>
        <ModalHeader />
        <ModalBody>
          <div className="text-center">
            <HiOutlineExclamationCircle className="mx-auto mb-4 h-14 w-14 text-primary-color dark:text-primary-color" />
            <h3 className="mb-5 text-lg font-normal text-primary-color dark:text-primary-color">
              {modalText}
            </h3>
            <div className="flex justify-center gap-4">
              <Button color="light-green" onClick={() => onClose()}>
               Dismiss
              </Button>
              
            </div>
          </div>
        </ModalBody>
      </Modal>
   
  );
}
