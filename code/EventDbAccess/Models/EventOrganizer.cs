using System;
namespace EventManagementDbAccess
{
    public class EventOrganizer
    {
        public int OrganizerId { get; set; }

        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }

        public string OrganizerWebsite { get; set; }

        public string OrganizerEventBaseUrl { get; set; }

        public string OrganizerDescription { get; set; }

        public byte[] OrganizerLogo { get; set; } = Array.Empty<byte>();
        public string OrganizerCity { get; set; }

        public string OrganizerCountry { get; set; }

        public string OrganizerPhone { get; set; }

        public string OrganizerStreetAddress { get; set; }

        public string OrgnaizerZipCode { get; set; }
        public string OrganizerInstagram { get; set; }=string.Empty;

        public string OrganizerFacebook { get; set; }=string.Empty;
        public string StripeAccountId { get; set; } = string.Empty;

        public StripeAccountStatus StripeConnectStatus { get; set; }= StripeAccountStatus.Inactive;
    }
    public enum StripeAccountStatus
    {
        IdCreated,
        LinkInitiated,
        Completed,
        Inactive,   
        Rejected,
        Deleted
    }
}