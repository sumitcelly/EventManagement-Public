using System;
using EventDbAccess;

public class SalesOrderConductor
{
    private readonly SalesOrderDbAccess _dbAccess;
    private readonly TicketAccess _ticketDbAccess;
    private readonly EventOrganizerDBAccess _eventOrganizerDbAccess;
    private readonly AttendeeDbAccess attendeeDbAccess;

    private readonly ILogger<SalesOrderConductor> _logger;
    public SalesOrderConductor(ILogger<SalesOrderConductor> logger, SalesOrderDbAccess dbAccess, TicketAccess ticketAccess,
                    EventOrganizerDBAccess eventOrganizerDbAccess, AttendeeDbAccess attendeeDbAccess)
    {
        if (dbAccess == null)
            throw new ArgumentNullException(nameof(dbAccess));
        if (ticketAccess == null)
            throw new ArgumentNullException(nameof(ticketAccess));
        if (eventOrganizerDbAccess == null)
            throw new ArgumentNullException(nameof(eventOrganizerDbAccess));
        if (attendeeDbAccess == null)
            throw new ArgumentNullException(nameof(attendeeDbAccess));

        _dbAccess = dbAccess;
        _ticketDbAccess = ticketAccess;
        _eventOrganizerDbAccess = eventOrganizerDbAccess;
        this.attendeeDbAccess = attendeeDbAccess;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SalesOrderConductor initialized.");
    }



    public async Task<string> CreateSalesOrder(CustomerSalesOrder customerSalesOrder)
    {
        if (customerSalesOrder == null)
            throw new ArgumentNullException(nameof(customerSalesOrder));
        if (customerSalesOrder.CustomerId <= 0)
            throw new ArgumentException("CustomerId cannot be null or empty.", nameof(customerSalesOrder.CustomerId));
        if (customerSalesOrder.EventId <= 0)
            throw new ArgumentException("EventId cannot be null or empty.", nameof(customerSalesOrder.EventId));
        if (customerSalesOrder.SalerOrderItems == null || customerSalesOrder.SalerOrderItems.Count == 0)
            throw new ArgumentException("SalerOrderItems cannot be null or empty.", nameof(customerSalesOrder.SalerOrderItems));
        if ((string.IsNullOrEmpty(customerSalesOrder.EmailAddress) ||
                string.IsNullOrWhiteSpace(customerSalesOrder.Name)) && customerSalesOrder.AttendeeId <= 0)
            throw new ArgumentException("EmailAddress  and Name should be provided if there is no signed in Attendee", nameof(customerSalesOrder.EmailAddress));

        Attendee attendee;
        if (customerSalesOrder.AttendeeId <= 0 && !string.IsNullOrWhiteSpace(customerSalesOrder.EmailAddress))
        {
            attendee = await attendeeDbAccess.GetAttendeeByEmail(customerSalesOrder.EmailAddress);
            if (attendee == null)
            {
                attendee = new Attendee
                {
                    Name = customerSalesOrder.Name,
                    Email = customerSalesOrder.EmailAddress,
                    Sms = string.Empty // Assuming SMS is not provided
                };
                customerSalesOrder.AttendeeId = await attendeeDbAccess.CreateAttendee(attendee);
                _logger.LogInformation($"New attendee created with ID: {customerSalesOrder.AttendeeId}");
            }
            else
            {
                _logger.LogInformation($"Existing attendee found with ID: {attendee.AttendeeId}");
            }
            customerSalesOrder.AttendeeId = attendee.AttendeeId;
        }
        else
        {
            // If AttendeeId is provided, fetch the existing attendee

             attendee = new Attendee(){ AttendeeId = customerSalesOrder.AttendeeId };
            _logger.LogInformation($"Existing attendee provided: {customerSalesOrder.AttendeeId}");
        }

        // Create the sales order
        var salesOrder = new SalesOrder
        {
            CustomerId = customerSalesOrder.CustomerId,
            EventId = customerSalesOrder.EventId,
            AttendeeId = customerSalesOrder.AttendeeId,
            DeliveryType = customerSalesOrder.DeliveryType,
        };
        // Save the sales order to the database
        int orderId = await _dbAccess.CreateSalesOrder(salesOrder);
        _logger.LogInformation($"Sales order created with ID: {orderId}");
        if (orderId <= 0)
            throw new Exception("Failed to create sales order.");
        //Create event sales items
        foreach (var item in customerSalesOrder.SalerOrderItems)
        {
            if (item.EventTicketTypeId <= 0)
                throw new ArgumentException("EventTicketTypeId cannot be null or empty.", nameof(item.EventTicketTypeId));
            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(item.Quantity));

            for (int i = 0; i < item.Quantity; i++)
            {
                var salesItem = new EventSalesItem
                {
                    SalesOrderId = orderId,
                    TicketScanned = 0, // Assuming ticket is not scanned initially
                    EventId = customerSalesOrder.EventId,
                    Attendee = attendee,
                    TicketTypeId = item.EventTicketTypeId,
                    TicketCode = EventUtils.PasswordGenerator.GetPassword()// Generate a unique ticket code
                };
                await _ticketDbAccess.AddEventTicket(salesItem);
                _logger.LogInformation($"Event sales item created with TicketCode: {salesItem.TicketCode}");
            }
        }
        
        return $"Sales order created successfully with Order ID: {orderId}";
       
    }
}