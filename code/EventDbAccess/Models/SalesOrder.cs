using EventManagementDbAccess;

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

    public SalesOrderStatus SalesOrderStatus { get; set; } = SalesOrderStatus.InProgress;

    public string  StripeSessionId { get; set; } = string.Empty;
}

public class UserSalesOrders : EventHeader
{
    public required string SalesOrderCode { get; set; }
    public int SalesOrdeId { get; set; }
}

public class SalerOrderReportItems
{
    public int OrderId { get; set;}
    public int OrderTotal { get; set;} = 0;
    public int OrderCount { get; set;} = 0;
    public DateTime OrderDate { get; set;} = DateTime.MinValue;
    public string  SalesOrderStatus { get; set;} = string.Empty;

    public string  EventName { get; set;} = string.Empty;

    public string FullName { get; set;} = string.Empty;
    public string EmailAddress { get; set;} = string.Empty;
}
public enum SalesOrderStatus
{
    InProgress, // No payment initiated yet
    PaymentRequired, //s Payment required but not initiated
    PaymentPending,//Payment session created, awaiting payment from customer
    CheckoutSessionCreated, // Payment initiated by user, wating for confirmation from Stripe
    PaymentFailed,
    PaymentSucceeded,
    OrderCompleted, // Payment succeeded and order is completed
    Refunded
}