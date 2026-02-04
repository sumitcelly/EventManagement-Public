public class EmailCampaignTemplate
{
    public int EventId { get; set; }
    public required string EmailTemplateName { get; set;}

    public int TemplateId { get; set; }

    public required string TemplateContent { get; set; }  

    public string? Description {get;set;}  

    public DateTime SendAt {get; set;}

    public bool SendNow {get; set;}

    public required string Subject { get; set; }

    public string? Status {get; set;}


}