using System;
using Amazon.S3.Model;

public class CustomerSalesOrder
{
    public int CustomerId { get; set; }
    public int EventId { get; set; }
    public int UserId { get; set; } = 0;
    public string EmailAddress { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = "Email"; // Default to Email"
    public string SalesOrderCode { get; set; } = string.Empty;
    public string SalesOrderQrCodeImage { get; set; } = string.Empty;

    public string CheckoutSessionId {get;set;} = string.Empty;
    public string CheckoutSessionSecret {get;set;} = string.Empty;
    public string CheckoutSessionPublishableKey {get;set;} = string.Empty;
    public int SalesOrderId { get; set; } = 0;
    public List<SalesOrderItems> SalesOrderItems { get; set; } = new List<SalesOrderItems>();

    public List<ErrorResponseSalesOrderItems> SalesOrderItemsError { get; set; } = new List<ErrorResponseSalesOrderItems>();

    // Additional properties can be added as needed

    public bool SimulationMode { get; set; } = false;
}

public class ErrorResponseSalesOrderItems
{
    public int EventItemTypeId { get; set; }

    public string Error { get; set; } = string.Empty;

    public bool PaymentRequired {get; set; } = false;
}

public class SalesOrderItems
{
    public decimal Cost { get; set; } = 0;
    public int EventTicketTypeId { get; set; }
    public int Quantity { get; set; } = 1; // Default to 1
}