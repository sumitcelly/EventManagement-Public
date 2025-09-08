export type Ticket = {
  id: number;
  name: string;
  description: string;
  cost: number;
  quantity: number;
};

export type TicketFormValues = {
  tickets: Ticket[];
  fullname: string;
  email:string ;
};
