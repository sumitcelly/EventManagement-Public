
import { Button, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { HiOutlineExclamationCircle } from "react-icons/hi";

export function InfoModal({modalText,openModal,onClose}:{modalText:string, openModal:boolean, onClose:()=>void}) {
  return (
    <Modal show={openModal} size="md" onClose={() => onClose()} popup>
      <ModalHeader />
      <ModalBody>
        <div className="text-center bg-white dark:bg-gray-900 rounded-lg p-2">
          <HiOutlineExclamationCircle className="mx-auto mb-4 h-14 w-14 text-primary-color dark:text-primary-color" />
          <h3 className="mb-5 text-lg font-normal text-primary-color dark:text-primary-color">
            {modalText}
          </h3>
          <div className="flex justify-center gap-4">
            <button
              type="button"
              onClick={() => onClose()}
              className="rounded bg-brand-dark px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
            >
              Dismiss
            </button>
          </div>
        </div>
      </ModalBody>
    </Modal>
  );
}
