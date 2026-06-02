using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


public static class PdfGenerator
{
    static PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;    
    }

    static public byte[] GenerateTicketsWithSkiaSharp(TicketPdfData pdfData, string platformName, string platformUrl)
    {
        if (pdfData == null) throw new ArgumentNullException(nameof(pdfData), "PDF data cannot be null.");
        if (string.IsNullOrEmpty(pdfData.EventOrganizerName) ||
            string.IsNullOrWhiteSpace(pdfData.EventDate) ||
            string.IsNullOrWhiteSpace(pdfData.EventName) ||
            string.IsNullOrWhiteSpace(pdfData.EventLocation) || 
            string.IsNullOrWhiteSpace(pdfData.OrderHolderEmail) ||
            string.IsNullOrWhiteSpace(pdfData.OrderHolderName) ||
            pdfData.Tickets == null || pdfData.Tickets?.Count ==0)
             throw new ArgumentException("One or more fields in PDF data are missing or invalid.");
      
   
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin((float)1.5, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Liberation Sans").FontSize(11).FontColor(Colors.Grey.Darken3));

                // HEADER: Repeats on every single page
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(leftCol =>
                        {
                            leftCol.Item().Text(platformName).FontSize(9).FontColor(Colors.Blue.Darken1).Medium();
                            leftCol.Item().Text(platformUrl).FontSize(9).FontColor(Colors.Grey.Darken1);
                            leftCol.Item().Text($"Organizer: {pdfData.EventOrganizerName}").FontSize(10).Bold();
                        });
              
                        row.RelativeItem().Column(rightCol =>
                        {
                            rightCol.Item().Text(pdfData.EventName).FontSize(14).Bold().FontColor(Colors.Blue.Darken4).AlignRight();
                            
                            rightCol.Item().Text($"{pdfData.EventDate}").FontSize(9).AlignRight();
                            rightCol.Item().PaddingTop(2);
                            rightCol.Item().Text($"{pdfData.EventLocation}").FontSize(9).AlignRight();
                        });
                    });

                    headerCol.Item().PaddingTop(8).PaddingBottom(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1).LineDashPattern([2,1]);
                });

                // CONTENT: One ticket container per page
                page.Content().Column(column =>
                {
                    for (int i = 0; i < pdfData.Tickets.Count; i++)
                    {
                        var ticket = pdfData.Tickets[i];
                        
                        // CRUCIAL: Pass data to your SkiaSharp drawing loop
                        //byte[] barcodeImageBytes = QRCodeUtils.GetQRCodes(ticket.TicketQrCode);

                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(ticketCol =>
                            {
                                ticketCol.Item().Text($"Ticket {i + 1} of {pdfData.Tickets.Count}").FontSize(10).FontColor(Colors.Grey.Darken1).Bold();
                                ticketCol.Item().PaddingBottom(8);
                                ticketCol.Item().Text(pdfData.OrderHolderName).FontSize(16).Bold().FontColor(Colors.Grey.Darken4);
                                ticketCol.Item().PaddingBottom(15);
                                ticketCol.Item().Text($"Ticket ID: {ticket.TicketQrCode}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                ticketCol.Item().Text($"Ticket Type: {ticket.TicketType}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            // Embed your native Linux SkiaSharp generated graphic here
                            row.ConstantItem(120).AlignMiddle().Image(Convert.FromBase64String(ticket.TicketQrCodeImage));
                        });

                        // One ticket page restriction rule
                        if (i < pdfData.Tickets.Count - 1)
                        {
                            column.Item().PageBreak();
                        }
                    }
                });

                // FOOTER: Standard pagination layout
                page.Footer().Column(footerCol =>
                {
                    footerCol.Item().PaddingTop(15).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2).LineDashPattern([1,1]);
                    footerCol.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Please show this document on your device for entry gating.")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(10);
                            text.CurrentPageNumber().FontSize(10); // Natively inserts the active integer page
                            text.Span(" of ").FontSize(10);
                            text.TotalPages().FontSize(10);        // Natively evaluates total pages array length
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
