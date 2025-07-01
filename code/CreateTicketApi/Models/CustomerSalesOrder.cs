using System;

public class CustomerSalesOrder
{
    public int CustomerId { get; set; }
    public int EventId { get; set; }
    public int AttendeeId { get; set; } = 0;
    public string EmailAddress { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = "Email"; // Default to Email"
    public string SalesOrderCode { get; set; }=string.Empty;
    public string SalesOrderQrCodeImage { get; set; } = string.Empty;
    public List<SalesOrderItems> SalesOrderItems { get; set; } = new List<SalesOrderItems>();

    // Additional properties can be added as needed
}

public class SalesOrderItems
{
    public int EventTicketTypeId { get; set; }
    public int Quantity { get; set; } = 1; // Default to 1
}