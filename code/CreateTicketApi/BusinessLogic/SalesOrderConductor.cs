using System;
using EventManagementDbAccess;
using EventUtils;
namespace CreateTicketApi.BusinessLogic;
public class SalesOrderConductor
{
    private readonly SalesOrderDbAccess _dbAccess;
    private readonly TicketAccess _ticketDbAccess;
    private readonly AttendeeDbAccess attendeeDbAccess;

    private readonly ILogger<SalesOrderConductor> _logger;

    private readonly EmailUtils _emailUtils;
    public SalesOrderConductor(ILogger<SalesOrderConductor> logger, SalesOrderDbAccess dbAccess, TicketAccess ticketAccess,
                    EventOrganizerDBAccess eventOrganizerDbAccess, AttendeeDbAccess attendeeDbAccess,
                    EmailUtils emailUtils)
    {
        if (dbAccess == null)
            throw new ArgumentNullException(nameof(dbAccess));
        if (ticketAccess == null)
            throw new ArgumentNullException(nameof(ticketAccess));
        if (eventOrganizerDbAccess == null)
            throw new ArgumentNullException(nameof(eventOrganizerDbAccess));
        if (attendeeDbAccess == null)
            throw new ArgumentNullException(nameof(attendeeDbAccess));

        if (emailUtils == null)
            throw new ArgumentNullException(nameof(emailUtils));
        _dbAccess = dbAccess;
        _ticketDbAccess = ticketAccess;
        this.attendeeDbAccess = attendeeDbAccess;
        _emailUtils = emailUtils;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SalesOrderConductor initialized.");
    }



    public async Task<bool> DeleteSalesOrder(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException("OrderId cannot be null or empty.", nameof(orderId));
      
        // Delete the sales order
        bool result = await _dbAccess.DeleteSalesOrder(orderId);
        if (!result)
            throw new Exception($"Failed to delete sales order with ID {orderId}.");
        return true;
    }

    public async Task<IEnumerable<EventSalesItem>> GetSalesOrderById(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException("OrderId cannot be null or empty.", nameof(orderId));

        var salesOrder = await _dbAccess.GetSalesOrderById(orderId);
        if (salesOrder == null)
            throw new Exception($"Sales order with ID {orderId} not found.");
        // Fetch the event sales items associated with the sales order
        IEnumerable<EventSalesItem> eventSalesItems = await _ticketDbAccess.GetEventTicketBySalesOrderId(salesOrder.OrderId, salesOrder.EventId);
        if (eventSalesItems == null || !eventSalesItems.Any())
            throw new Exception($"No event sales items found for sales order ID {orderId}.");
        eventSalesItems.ToList().ForEach(item =>
        {
            item.QRBase64Image = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(item.TicketCode));
            item.TicketCode = string.Empty; // Clear the ticket code for security reasons
            //var t= QRCodeUtils.GetQRText(Convert.FromBase64String(item.QRBase64Image)); // Decode the QR code to ensure it's valid
        });
        return eventSalesItems;
    }
    public async Task<CustomerSalesOrder> CreateSalesOrder(CustomerSalesOrder customerSalesOrder)
    {
        if (customerSalesOrder == null)
            throw new ArgumentNullException(nameof(customerSalesOrder));
        if (customerSalesOrder.CustomerId <= 0)
            throw new ArgumentException("CustomerId cannot be null or empty.", nameof(customerSalesOrder.CustomerId));
        if (customerSalesOrder.EventId <= 0)
            throw new ArgumentException("EventId cannot be null or empty.", nameof(customerSalesOrder.EventId));
        if (customerSalesOrder.SalesOrderItems == null || customerSalesOrder.SalesOrderItems.Count == 0)
            throw new ArgumentException("SalesOrderItems cannot be null or empty.", nameof(customerSalesOrder.SalesOrderItems));
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

            attendee = new Attendee() { AttendeeId = customerSalesOrder.AttendeeId };
            _logger.LogInformation($"Existing attendee provided: {customerSalesOrder.AttendeeId}");
        }

        // Create the sales order
        var salesOrder = new SalesOrder
        {
            CustomerId = customerSalesOrder.CustomerId,
            EventId = customerSalesOrder.EventId,
            AttendeeId = customerSalesOrder.AttendeeId,
            DeliveryType = customerSalesOrder.DeliveryType,
            SalesOrderCode = PasswordGenerator.GetPassword(), // Generate a unique sales order code
        };
        // Save the sales order to the database
        int orderId = await _dbAccess.CreateSalesOrder(salesOrder);
        _logger.LogInformation($"Sales order created with ID: {orderId}");
        if (orderId <= 0)
            throw new Exception("Failed to create sales order.");
        //Create event sales items
        foreach (var item in customerSalesOrder.SalesOrderItems)
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
                    EventItemType = new EventItemType
                    {
                        EventItemTypeId = item.EventTicketTypeId,

                    },
                    TicketCode = EventUtils.PasswordGenerator.GetPassword()// Generate a unique ticket code
                };
                await _ticketDbAccess.AddEventTicket(salesItem);
                _logger.LogInformation($"Event sales item created with TicketCode: {salesItem.TicketCode}");
            }
        }
        await _emailUtils.SendOrderConfirmationEmail(salesOrder, attendee);
        return new CustomerSalesOrder()
        {
            SalesOrderCode = salesOrder.SalesOrderCode,
            SalesOrderQrCodeImage = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(salesOrder.SalesOrderCode))
        };
    }
    
    
}