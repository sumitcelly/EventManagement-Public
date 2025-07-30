
using System;

public class PaymentLineItemModel
{
    public int EventTicketTypeId { get; set; }
    public int Quantity { get; set; } = 1; // Default to 1
    public decimal Price { get; set; } = 0.0m; // Default to 0.0
    public string Description { get; set; } = string.Empty;

    // Additional properties can be added as needed
}