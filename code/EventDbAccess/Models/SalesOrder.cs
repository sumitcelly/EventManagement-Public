public class SalesOrder
{
    
    public int OrderId { get; set; } 

    public int CustomerId { get; set; } 

    public int EventId { get; set; }

    public int AttendeeeId { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}