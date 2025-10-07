export interface EventFullInfo {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventDescription: string;
  eventOrganizer: number;
  eventLocation: string;
  eventHeadliner: string;
  isFree: boolean
}

export interface EventHeader {
  eventId: number;
  eventName: string;
  eventDate: Date;
  eventLocation: string;
  eventOrganizerId:number;
  isLive?:boolean;
}
