import { Button, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { useState, useEffect } from "react";

export function RichTextEditorModal({
  modalTitle,
  openModal,
  onClose,
  onConfirm,
  initialContent = ""
}: {
  modalTitle: string;
  openModal: boolean;
  onClose: () => void;
  onConfirm: (content: string) => void;
  initialContent?: string;
}) {

    
  const [content, setContent] = useState(initialContent);
  console.log('Initial content for modal', initialContent);

  useEffect(() => {
    setContent(initialContent);
  }, [initialContent, openModal]);

  const handleConfirm = () => {
    onConfirm(content);
    setContent("");
  };

  const handleClose = () => {
    setContent("");
    onClose();
  };

  return (
    <Modal show={openModal} size="xl" onClose={handleClose} popup className="w-full">
      <ModalHeader>{modalTitle}</ModalHeader>
      <ModalBody>
        <div className="space-y-4 w-full">
          <div 
            className="border rounded p-4 bg-white prose prose-sm max-w-none w-full break-words overflow-x-auto"
            dangerouslySetInnerHTML={{ __html: content }}
          />
          <div className="flex justify-end gap-4">
            <Button color="alternative" onClick={handleClose}>
              Close
            </Button>
          </div>
        </div>
      </ModalBody>
    </Modal>
  );
}
