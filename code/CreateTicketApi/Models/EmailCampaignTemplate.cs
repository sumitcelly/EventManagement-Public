public class EmailCampaignTemplate
{
    public int EventId { get; set; }

    public required string EmailCampaignName { get;set;}
    public string? Description { get; set; }
    public int TemplateId { get; set; }

    public required string TemplateContent { get; set; }  

    public DateTime SendAt {get; set;}

    public bool SendNow {get; set;}

    public required string Subject { get; set; }

    public string? Status {get; set;}

    public bool TemplateContentChange {get;set;}

    public string EventName { get; set; } = "";
}