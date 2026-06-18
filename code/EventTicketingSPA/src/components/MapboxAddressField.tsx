import React, { useEffect, useRef } from "react";
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
  const inputRef = useRef<HTMLInputElement>(null);
  const lastProcessedValue = useRef<string>("");
  
  //this maybe a hack here. Without this for sure, the  full address does not get set.
  //Only the street address gets set. Not sure why.
  useEffect(() => {
    if (inputRef.current && inputRef.current.value !== value) {
      inputRef.current.value = value || "";
      lastProcessedValue.current = value || "";
    }
  }, [value]);


  const handleRetrieve = (res: any) => {
    
    console.log('inside handle retrieve',res);
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

    //onSelect(addressData);
    setTimeout(() => onSelect(addressData), 0);
    lastProcessedValue.current = addressData.fullAddress;
    
    console.log("Address data:", addressData);
  };

  // Handle browser autocomplete selection (Google Maps in Chrome)
  const handleInputChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    
    // Only trigger if value actually changed and is not empty
    if (newValue && newValue !== lastProcessedValue.current) {
      console.log("Browser autocomplete detected:", newValue);
      lastProcessedValue.current = newValue;
      
      // Use Mapbox Geocoding API to get structured address data
      try {
        const encodedAddress = encodeURIComponent(newValue);
        const response = await fetch(
          `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodedAddress}.json?access_token=${MAPBOX_TOKEN}&types=address&limit=1`
        );
        
        if (response.ok) {
          const data = await response.json();
          console.log("Full Mapbox response:", data);
          const feature = data.features?.[0];
          
          if (feature) {
            console.log("Selected feature:", feature);
            const coords = feature.geometry?.coordinates;
            
            // Extract address components from context array
            const context = feature.context || [];
            let city = "";
            let state = "";
            let zip = "";
            
            // Parse context array to get city, state, zip
            for (const item of context) {
              if (item.id.startsWith("place.")) {
                city = item.text;
              } else if (item.id.startsWith("region.")) {
                // Safely extract state code - handle both "US-CO" and standalone codes
                state = item.short_code ? item.short_code.split('-').pop() : item.text;
              } else if (item.id.startsWith("postcode.")) {
                zip = item.text;
              }
            }
            
            // Build street address safely - use both address number and street name if available
            let street = "";
            if (feature.address && feature.text) {
              street = `${feature.address} ${feature.text}`;
            } else {
              street = feature.address || feature.text || "";
            }
            
            const addressData: AddressData = {
              fullAddress: feature.place_name || "",
              street: street,
              city: city,
              state: state,
              zip: zip,
              lat: coords?.[1],
              lng: coords?.[0],
            };
            
            console.log("Parsed address data:", addressData);
            setTimeout(() => onSelect(addressData), 0);
            return;
          }
        }
      } catch (error) {
        console.error("Error geocoding browser autocomplete address:", error);
      }
      
      // Fallback if geocoding fails
      const addressData: AddressData = {
        fullAddress: newValue,
        street: newValue,
      };
      
      setTimeout(() => onSelect(addressData), 0);
    }
  };

  return (
    <AddressAutofillFixed accessToken={MAPBOX_TOKEN} 
      //onClick={(e:any)=>{e.stopPropagation();}}
     onRetrieve= {handleRetrieve} options={{ types: "address" }}
      >
      <input
       ref={inputRef}
        name="fullAddress"
        type="text"
        placeholder="Enter an address"
        autoComplete="street-address"
        className="border rounded w-full p-2"
        defaultValue={value}
        onChange={handleInputChange}
        
      />
    </AddressAutofillFixed>
  );
}
