public class Attendee
{
    public int AttendeeId { get; set; } = 0;

    public string Name { get; set; }

    public string Email { get; set; }

    public string Sms { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string StreetAddress { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}