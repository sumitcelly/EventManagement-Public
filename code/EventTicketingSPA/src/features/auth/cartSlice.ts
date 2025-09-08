// src/features/auth/authSlice.js
import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { Ticket } from "../../types/Tickets";
import { TicketFormValues } from "../../types/Tickets"; 


const initialState: TicketFormValues = {
  tickets: [],
  fullname: '',
  email: '',
};

// Async login action

const cartSlice = createSlice({
  name: "cart",
  initialState,
  reducers: {
    updatebuyer(state,action:PayloadAction<{fullname:string,email:string}>) {
      state.email = action.payload.email;
      state.fullname = action.payload.fullname
    },
    updatetickets(state,action:PayloadAction<{tickets:Ticket[]}>) {
      state.tickets = action.payload.tickets;
    },
    resetCart(state) {
      state =initialState;
    },
  },

});

export const { updatebuyer, updatetickets, resetCart } = cartSlice.actions;
export default cartSlice.reducer;

