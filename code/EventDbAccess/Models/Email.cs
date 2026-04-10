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

        public string ErrorMessage { get; set; } = string.Empty;
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

    // public class EmailStatusUpdate
    // {
    //     public int Id { get; set;}
    //     public int  CampaignId { get; set; }

    //     public required string RecipientEmail { get; set; } 

    //     public required string Status { get; set; }

    //     public string ErrorMessage { get; set; } = string.Empty;

    //     public string SenderMessageId { get; set; } = string.Empty;
    // }

    public class EmailTransactionLog
    {
        public int Id { get; set; }// the pk of the emailtransactionlog table, used for updating status of transactional emails
        public required string RecipientEmail { get; set; }
        public string EmailType { get; set; } = "OrderConfirmation";// EmailVerification, PasswordReset, etc.
        public required int RefId { get; set; } // can be salesorderdid or userid depending on email type
        public string SenderMessageId { get; set; } = string.Empty;
        public string Status { get; set; } = "Queued";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

}