namespace EventManagementDbAccess;

public class EventOrganizerMembers
{
    public int OrganizerMemberId { get; set; }
    public int CustomerId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public EventOrganizerMembers()
    {
    }
}