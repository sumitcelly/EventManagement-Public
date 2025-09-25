//Types related to tickets and ticket forms
export type Ticket = {
  eventItemTypeId: number;
  name: string;
  description: string;
  cost: number;
  quantity: number;
  maxPerOrder?:number;
};


export type TicketFormValues = {
  tickets: Ticket[];
  fullname: string;
  email:string ;
};

// For displaying tickets in the order confirmation page or when tickets are purchased
export type EventTickets = {
  id: number;
  name: string;
  qrCode: string;
  quantity?: number;
};
