import React, { useEffect, useRef } from "react";
import { AddressAutofill } from "@mapbox/search-js-react";

type RetrieveResponse = Parameters<
  NonNullable<React.ComponentProps<typeof AddressAutofill>["onRetrieve"]>
>[0];

type Props = {
  onSelect: (data: AddressData) => void;
  onAddressChange: (value: string) => void;

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

const AddressAutofillFixed = AddressAutofill as unknown as React.FC<any>;

export default function MapboxAddressField({ onSelect, onAddressChange, value }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const pendingLookup = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const lookupController = useRef<AbortController | undefined>(undefined);
  const lastSelectedValue = useRef("");
  
  useEffect(() => {
    if (inputRef.current && inputRef.current.value !== value) {
      inputRef.current.value = value || "";
    }
  }, [value]);

  const resolveAddress = async (query: string) => {
    if (!query.trim() || query.trim() === lastSelectedValue.current) return;

    lookupController.current?.abort();
    const controller = new AbortController();
    lookupController.current = controller;

    try {
      const params = new URLSearchParams({
        access_token: MAPBOX_TOKEN,
        types: "address",
        country: "us",
        limit: "1",
      });
      const response = await fetch(
        `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(query)}.json?${params}`,
        { signal: controller.signal }
      );

      if (!response.ok) {
        throw new Error(`Mapbox geocoding failed: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      const feature = data.features?.[0];
      if (!feature || controller.signal.aborted) return;

      const context: Array<{ id: string; text: string; short_code?: string }> = feature.context || [];
      const region = context.find((item) => item.id.startsWith("region."));
      const coords = feature.geometry?.coordinates;
      const addressData: AddressData = {
        fullAddress: feature.place_name || query,
        street: feature.address && feature.text
          ? `${feature.address} ${feature.text}`
          : feature.address || feature.text || query,
        city: context.find((item) => item.id.startsWith("place."))?.text || "",
        state: region?.short_code ? region.short_code.split("-").pop() : region?.text || "",
        zip: context.find((item) => item.id.startsWith("postcode."))?.text || "",
        lat: coords?.[1],
        lng: coords?.[0],
      };

      lastSelectedValue.current = query;
      onSelect(addressData);
    } catch (error) {
      if (!controller.signal.aborted) {
        console.error("Error geocoding completed address:", error);
      }
    }
  };

  const handleRetrieve = (res: RetrieveResponse) => {
    clearTimeout(pendingLookup.current);
    lookupController.current?.abort();
    const feature = res.features?.[0];
    if (!feature) return;
    
    const properties = feature.properties;
    const context: Array<{ id: string; text: string }> = properties.context || [];
    const coords = feature.geometry?.coordinates;
    const city = context.find((item) => item.id.startsWith("place."))?.text;
    const state = context.find((item) => item.id.startsWith("region."))?.text;
    const zip = context.find((item) => item.id.startsWith("postcode."))?.text;
    const addressData: AddressData = {
      fullAddress: properties.full_address || properties.place_name || "",
      street: properties.address_line1 || properties.address || properties.feature_name || "",
      city: properties.address_level2 || city || "",
      state: properties.address_level1 || state || "",
      zip: properties.postcode || zip || "",
      lat: coords?.[1],
      lng: coords?.[0],
    };

    lastSelectedValue.current = addressData.fullAddress;
    onSelect(addressData);
    setTimeout(() => {
      if (inputRef.current && addressData.fullAddress) {
        inputRef.current.value = addressData.fullAddress;
        onAddressChange(addressData.fullAddress);
      }
    }, 0);
  };

  const scheduleAddressLookup = (query: string) => {
    clearTimeout(pendingLookup.current);
    pendingLookup.current = setTimeout(() => {
      void resolveAddress(query);
    }, 150);
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const query = event.currentTarget.value;
    onAddressChange(query);

    const inputEvent = event.nativeEvent as InputEvent;
    if (inputEvent.inputType === "insertReplacementText" || inputEvent.inputType === "insertFromPaste") {
      scheduleAddressLookup(query);
    }
  };

  return (
    <AddressAutofillFixed accessToken={MAPBOX_TOKEN} 
      onRetrieve={handleRetrieve} options={{ country: "us" }}
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
        onBlur={(event) => scheduleAddressLookup(event.currentTarget.value)}
        
      />
    </AddressAutofillFixed>
  );
}
