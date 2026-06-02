
public class TicketPdfData
{
    public required string EventOrganizerName { get; set; }

    public required string EventOrganizerEmail { get;set;}

    public required string EventName { get; set; }

    public required string EventDate { get; set; }

    public required string EventLocation { get; set; }

    public required string OrderHolderName { get; set; }

    public required string OrderHolderEmail { get; set; }

    public required List<TicketDetails> Tickets { get; set; } = new List<TicketDetails>();

}


public class TicketDetails
{
    public required string TicketQrCode { get;set;}

    public required string TicketQrCodeImage { get; set; }

    public required string TicketType { get; set; }
}