public class SalesOrder
{
    
    public int OrderId { get; set; } 

    public int CustomerId { get; set; } 

    public int EventId { get; set; }

    public int UserId { get; set; } 

    public string SalesOrderCode { get; set; } = string.Empty;

    public string DeliveryType { get; set; } = "Email"; // Default to Email

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}