import React, { useRef } from "react";
import { AddressAutofill } from "@mapbox/search-js-react";

type Props = {
  onSelect: (data: AddressData) => void;

  value?: string;
};

export type AddressData = {
  fullAddress: string;
  street?: string;
  city?: string;
  state?: string;
  zip?: string;
  lat?: number;
  lng?: number;
};

const MAPBOX_TOKEN = import.meta.env.VITE_MAPBOX_TOKEN;

// ✅ TypeScript sometimes doesn't infer this JSX component correctly
const AddressAutofillFixed = AddressAutofill as unknown as React.FC<any>;

export default function MapboxAddressField({ onSelect,  value }: Props) {
  //const inputRef = useRef<HTMLInputElement>(null);

  const handleRetrieve = (res: any) => {
    const feature = res.features?.[0];
    if (!feature) return;
    
    const ctx = feature.properties;
    const coords = feature.geometry?.coordinates;
    console.log("Address selected:", feature);
    const addressData: AddressData = {
      fullAddress: ctx?.place_name || "",
      street: ctx?.address_line1 || "",
      city: ctx?.place || ctx?.city || "",
      state: ctx?.region_code || "",
      zip: ctx?.postcode || "",
      lat: coords?.[1],
      lng: coords?.[0],
    };

    onSelect(addressData);
   //console.log("Address data:", addressData);
    // if (inputRef.current) {
    //   inputRef.current.value = addressData.fullAddress || "";
    // }
    // console.log("Address data:", inputRef.current?.value);
  };

  return (
    <AddressAutofillFixed accessToken={MAPBOX_TOKEN} onRetrieve={handleRetrieve}>
      <input
       // ref={inputRef}
        name="address"
        type="text"
        placeholder="Enter an address"
        autoComplete="address-line1"
        className="border rounded w-full p-2"
        defaultValue={value}
        
      />
    </AddressAutofillFixed>
  );
}
