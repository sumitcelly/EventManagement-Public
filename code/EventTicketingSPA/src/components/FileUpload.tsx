import React from 'react'

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
 

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return;

    console.log('file type and name', file.type, file.name);

    if (file.size > 5 * 1024 * 1024) { // 5MB limit
      alert('File size must be less than 5 MB.')
      return;
    }

    // extract extension from filename
    const nameParts = file.name.split('.')
    const ext = nameParts.length > 1 ? nameParts.pop()!.toLowerCase() : '';

    // read just the first 512 bytes to detect signature (no need to read entire file)
    const head = file.slice(0, 512);
    const buffer = await head.arrayBuffer();
    const bytes = new Uint8Array(buffer);
  
    const detectType = () => {
      if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) return 'jpg';
      if (bytes.length >= 8 && bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47) return 'png';
      if (bytes.length >= 4 && bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x38) return 'gif';
      if (bytes.length >= 12 && bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x46 && bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50) return 'webp';
      if (bytes.length >= 2 && bytes[0] === 0x42 && bytes[1] === 0x4d) return 'bmp';
      // try to detect svg from text
      try {
        const text = new TextDecoder().decode(bytes.slice(0, 200));
        if (text.trim().startsWith('<') && /<svg|<\?xml/i.test(text)) return 'svg';
      } catch {
        /* ignore */
      }
      return '';
    }

    const detected = detectType();

    if (!detected) {
      alert('Unsupported or unknown image format.');
      return;
    }

    // normalize extension names
    const extNormalized = ext === 'jpeg' ? 'jpg' : ext;

    if (ext && extNormalized !== detected) {
      alert(`File extension .${ext} does not match actual file type (${detected}).`);
      return;
    }

    // all good — create preview
    const reader = new FileReader();
    reader.onloadend = () => {
      setImagePreview(reader.result as string);
      setFile(file);
    }
    reader.readAsDataURL(file);
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
