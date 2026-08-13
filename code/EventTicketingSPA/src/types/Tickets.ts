//Types related to tickets and ticket forms
export type Ticket = {
  eventItemTypeId: number;
  name: string;
  description: string;
  cost: number;
  //number of tickets a user wants to buy
  quantity: number;
  maxPerOrder?:number;
  ticketsSold:number;
  //upper limit of tickets that can be sold for this ticket type
  totalAllowed:number;
  salesStartDate: Date;
  salesEndDate: Date;
  ticketValidFromDate?: Date;
  ticketValidToDate?: Date;
};


export type TicketFormValues = {
  tickets: Ticket[];
  fullname: string;
  email:string ;
  zipCode:string;
};

// For displaying tickets in the order confirmation page or when tickets are purchased
export type EventTickets = {
  id: number;
  name: string;
  qrCode: string;
  quantity?: number;
};


