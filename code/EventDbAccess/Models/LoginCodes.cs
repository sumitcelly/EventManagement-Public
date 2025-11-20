namespace EventManagementDbAccess
{
    public class LoginCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SecurityCode { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? RequestIp { get; set; }
        public DateTime? UsedAt { get; set; } 
    }
}