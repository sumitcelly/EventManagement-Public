
using System;


public class PaymentModel
{


    public List<PaymentLineItemModel> LineItems { get; set; } = new List<PaymentLineItemModel>();
   
    public int EventId { get; set; }
    public int SalesOrderId { get; set; }
    // public required  string EventStreetAddress { get; set; } 
    // public required string EventCity { get; set;}
    // public required string EventPostalCode { get; set;} 
    // public required string EventState { get; set;} 

    // public required string EventCountry { get; set;} ="US";

    public required string EventCategory { get; set; }

    public required string LocationId { get; set; }

}
public class PaymentLineItemModel
{
    public int EventTicketTypeId { get; set; }
    public int Quantity { get; set; } = 1; // Default to 1
    public decimal Price { get; set; } = 0.0m; // Default to 0.0
    public string Description { get; set; } = string.Empty;

    public bool IsAddOn { get; set; } = false; // Default to false, can be set to true for add-ons

    // Additional properties can be added as needed
}