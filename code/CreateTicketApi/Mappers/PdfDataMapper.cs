using EventManagementDbAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreateTicketApi.Mappers
{
    public static class PdfDataMapper
    {
        /// <summary>
        /// Maps EventHeader and EventSalesItem list to TicketPdfData
        /// </summary>
        /// <param name="eventDetails">Event header information</param>
        /// <param name="eventTickets">List of tickets/sales items</param>
        /// <param name="organizerEmail">Email of the event organizer (optional, can be fetched separately)</param>
        /// <returns>Mapped TicketPdfData object</returns>
        public static TicketPdfData MapToTicketPdfData(
            EventHeader eventDetails, 
            IEnumerable<EventSalesItem> eventTickets,
            string organizerEmail = "")
        {
            if (eventDetails == null)
                throw new ArgumentNullException(nameof(eventDetails), "Event details cannot be null");

            if (eventTickets == null || !eventTickets.Any())
                throw new ArgumentException("Event tickets cannot be null or empty", nameof(eventTickets));

            // Get first ticket for order holder info (all tickets in order are from same purchaser)
            var firstTicket = eventTickets.First();
            
            if (firstTicket.User == null)
                throw new InvalidOperationException("Ticket user information is missing");

            var pdfData = new TicketPdfData
            {
                EventOrganizerName = eventDetails.EventOrganizer ?? "Unknown Organizer",
                EventOrganizerEmail = organizerEmail,
                EventName = eventDetails.EventName,
                EventDate = eventDetails.EventDate.ToString("MMMM dd, yyyy"),
                EventLocation = eventDetails.EventLocation,
                OrderHolderName = firstTicket.User.Name,
                OrderHolderEmail = firstTicket.User.Email,
                Tickets = MapToTicketDetails(eventTickets).ToList()
            };

            return pdfData;
        }

        /// <summary>
        /// Maps EventSalesItem collection to TicketDetails collection
        /// </summary>
        private static IEnumerable<TicketDetails> MapToTicketDetails(IEnumerable<EventSalesItem> eventTickets)
        {
            return eventTickets.Select(ticket => new TicketDetails
            {
                TicketQrCodeImage = ticket.QRBase64Image ?? string.Empty,
                TicketQrCode = ticket.TicketCode ?? string.Empty,
                TicketType = ticket.EventItemType?.Name ?? "General Admission"
            }).ToList();
        }
    }
}
