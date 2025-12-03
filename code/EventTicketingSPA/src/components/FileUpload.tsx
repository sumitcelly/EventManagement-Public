import { useState } from 'react'

type Props = {
  imagePreview: string | null;
  setImagePreview: (preview: string | null) => void;
  // fileType: string | null;
  // setFileType :(fileType: string | null) => void;
  // fileName: string | null;
  // setFileName :(fileName: string | null) => void;
  file: File | null;
  setFile : (fileObj: File |null)=>void;
};

export default function ImageUploadBox({ imagePreview, setImagePreview,file, setFile }: Props) {
 

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      console.log('file type and name',file.type, file.name);

      const reader = new FileReader()
      reader.onloadend = () => {
        setImagePreview(reader.result as string);
        setFile(file);
      }
      reader.readAsDataURL(file)
    }
  }

  return (
    <div className="space-y-4">
      {/* Upload Control */}
      <label className="inline-block px-4 py-2 bg-gray-100 rounded-lg cursor-pointer hover:bg-gray-200 border border-gray-300">
        <span className="text-gray-700">Upload Image</span>
        <input
          type="file"
          accept="image/*"
          className="hidden"
          onChange={handleFileChange}
        />
      </label>

      {/* Preview Box */}
      {imagePreview && (
        <div className="border-2 border-gray-300 rounded-lg p-2 bg-gray-50 flex justify-center">
          <img
            src={imagePreview}
            alt="Preview"
            className="max-h-64 object-contain rounded"
          />
        </div>
      )}
    </div>
  )
}
