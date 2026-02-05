namespace EventManagementDbAccess
{
    public class EmailRecipient
    {
        public int Id { get; set; }
        public int EmailCampaignId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAttemptedAt { get; set; }
        public int? RetryCount { get; set; }
        public int SalesOrderId { get; set; } = 0;
    }
    public class EmailCampaign
    {
        public int Id { get; set; }

        public required string Name {get;set;}

        public string? Description {get; set;}
        public int TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public bool IsDefault { get; set; }
        public string? TemplateContent { get; set; }
        public string? TemplateDescription { get; set; }
        public string? Subject { get; set; }
        public string? EventName {get;set;}
        public int? EventId { get; set; }
        public DateTime SendAt { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}