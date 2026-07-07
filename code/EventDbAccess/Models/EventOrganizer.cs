using System;
namespace EventManagementDbAccess
{
    /// <summary>
    /// Even though everything here says event organizer, its really the event organization data. So read organizerName as organizationName.
    /// </summary>
    public class EventOrganizer
    {
        public int OrganizerId { get; set; }
        public required string OrganizationName { get; set; }
        public string OrganizerEmail { get; set; } = string.Empty;
        public string OrganizerWebsite { get; set; } = string.Empty;
        public required string OrganizerEventBaseUrl { get; set; }
        public string OrganizerDescription { get; set; } = string.Empty;

        public required string OrganizerAboutMe { get; set; }
        public string OrganizerInstagram { get; set; } = string.Empty;
        
        public string OrganizerImageUrl { get; set; } =string.Empty;
        public string OrganizerFacebook { get; set; } = string.Empty;
        public string OrganizerX { get; set; } = string.Empty;
        public string StripeAccountId { get; set; } = string.Empty;
        public string OrganizerPhone { get; set; } = string.Empty;
        public string OrganizerCountry { get; set; } = "US";

        public string StripeConnectStatus { get; set; } = string.Empty;

        public string OrganizationFullAddress { get; set; } = string.Empty;
        //Organizer Address fields
        public string OrganizerCity { get; set; } = string.Empty;
        public string OrganizerState { get; set; } = string.Empty;

        public string OrganizerStreetAddress { get; set; } = string.Empty;

        public string OrgnaizerZipCode { get; set; } = string.Empty;
    }
    public enum StripeAccountStatus
    {
        IdCreated,
        LinkInitiated,
        Completed,
        RequirementsPending,   
        InProgress,
        Deleted
    }
}