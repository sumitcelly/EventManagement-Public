// src/features/auth/authSlice.js

// This slice is for managing event details in the Redux store.
//  The event in question is the one the user has expressed in getting tickets for.
import { createSlice,  PayloadAction } from "@reduxjs/toolkit";
import {EventHeader} from "../../types/Event";
import { act } from "react";


const initialState: EventHeader = {
  eventId: 0,
  eventName: "",
  eventDate: new Date(),
  eventLocation: "",
  eventOrganizerId: 0

};

// Async login action

const eventSlice = createSlice({
  name: "event",
  initialState,
  reducers: {
    updateEvent(state,action:PayloadAction<{event:EventHeader}>) {
      return { ...state, ...action.payload.event };
    },
  
    resetEvent(state) {
      return initialState;
      {/* let the user info stay*/}
    },
  },

});

export const { updateEvent, resetEvent } = eventSlice.actions;
export default eventSlice.reducer;

