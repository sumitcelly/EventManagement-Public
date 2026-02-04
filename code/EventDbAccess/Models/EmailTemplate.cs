namespace EventManagementDbAccess
{
    
    public class EmailTemplate
    {
        public int Id { get; set; }
    
        public required string TemplateName { get; set; }
        public bool IsDefault { get; set; } = false;
        public required string TemplateContent { get; set; }
        public string? TemplateDescription { get; set; }
        public required string Subject { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}