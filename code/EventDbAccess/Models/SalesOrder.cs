using EventManagementDbAccess;

public class SalesOrder
{

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public string? SalesOrderCode { get; set; }

    public string DeliveryType { get; set; } = "Email"; // Default to Email

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public SalesOrderStatus SalesOrderStatus { get; set; } = SalesOrderStatus.InProgress;

    public string  StripeSessionId { get; set; } = string.Empty;

    public string PaymentIntentId { get; set; } =string.Empty;

    public string RefundId { get; set; } =string.Empty;


    public int RefundAmount { get; set; }

    public DateTime RefundedAt {get;set; }

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
    InProgress, // User is building the order(this is just the default status)
    Reserved, // /Payment session created, awaiting payment from customer
    Timedout, // USer did nt complete payment in time
    Cancelled,//User hit cancel on the UI
    Abandoned, // User  closed the browser 
    Replaced, // New order created to replace this one
    PaymentFailed,
    PaymentSucceeded,//stripe confirmed payment

    OrderFinalizationError, // There was an error finalizing the order(tickets couldnt be issued etc)
    OrderCompleted, //  and order is completed(payment was not required)
    RefundSuccess,
    RefundedPartially,
    RefundFailed,
    RefundUpdateDbError
}