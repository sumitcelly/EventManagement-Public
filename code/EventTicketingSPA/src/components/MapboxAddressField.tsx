import React from "react";
import { AddressAutofill } from "@mapbox/search-js-react";

type Props = {
  onSelect: (data: AddressData) => void;
  token: string;
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

// ✅ TypeScript sometimes doesn't infer this JSX component correctly
const AddressAutofillFixed = AddressAutofill as unknown as React.FC<any>;

export default function MapboxAddressField({ onSelect, token, value }: Props) {
  const handleRetrieve = (res: any) => {
    const feature = res.features?.[0];
    if (!feature) return;

    const ctx = feature.properties;
    const coords = feature.geometry?.coordinates;
console.log("Address selected:", feature);
    const addressData: AddressData = {
      fullAddress: feature.place_name,
      street: ctx?.address_line1 || "",
      city: ctx?.place || ctx?.city || "",
      state: ctx?.region || "",
      zip: ctx?.postcode || "",
      lat: coords?.[1],
      lng: coords?.[0],
    };

    onSelect(addressData);
  };

  return (
    <AddressAutofillFixed accessToken={token} onRetrieve={handleRetrieve}>
      <input
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
