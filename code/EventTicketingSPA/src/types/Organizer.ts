export interface OrganizerInfo {
  organizerId: number;
  organizationName: string;
  organizerName: string;
  organizationFullAddress?: string;
  organizerWebsite: string;
  organizerEventBaseUrl: string;
  organizerDescription: string;
  organizerAboutMe: string;
  organizerImageUrl:string;
  organizerInstagram?: string;
  organizerFacebook?: string;
  organizerX?:string;
  organizerPhone:string;
  organizerEmail:string;
  organizerCountry:string;
  organizerStripeAccountId?:string;
  organizerStripeAccountStatus?:string;
}

