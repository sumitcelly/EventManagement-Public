import React from "react";

export interface SelectItem {
  id: number | string;
  label: string;
}

interface Props {
  value: string | number;
  onChange: (value: string | number) => void;
  options: SelectItem[];
  placeholder?: string;
}

const DynamicSelect: React.FC<Props> = ({ value, onChange, options, placeholder }) => {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="border rounded p-2 min-w-[200px]"
    >
      <option value="">{placeholder || "Select an option"}</option>

      {options.map((opt) => (
        <option key={opt.id} value={opt.id}>
          {opt.label}
        </option>
      ))}
    </select>
  );
};

export default DynamicSelect;
