// src/features/auth/authSlice.js
import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { Ticket } from "../../types/Tickets";
import { TicketFormValues } from "../../types/Tickets"; 


const initialState: TicketFormValues = {
  tickets: [],
  fullname: '',
  email: '',
  zipCode:''
};

// Async login action

const cartSlice = createSlice({
  name: "cart",
  initialState,
  reducers: {
    updatebuyer(state,action:PayloadAction<{fullname:string,email:string,zipCode:string}>) {
      state.email = action.payload.email;
      state.fullname = action.payload.fullname;
      state.zipCode = action.payload.zipCode;
    },
    updatetickets(state,action:PayloadAction<{tickets:Ticket[]}>) {
      state.tickets = action.payload.tickets;
    },
    resetCart(state) {
      state.tickets = [];
      {/* let the user info stay*/}
    },
  },

});

export const { updatebuyer, updatetickets, resetCart } = cartSlice.actions;
export default cartSlice.reducer;

