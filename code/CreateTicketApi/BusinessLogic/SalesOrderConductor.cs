using System;
using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.SignalR.Protocol;
using Mysqlx.Crud;
namespace CreateTicketApi.BusinessLogic;
public class SalesOrderConductor
{
    private readonly SalesOrderDbAccess _dbAccess;
    private readonly TicketAccess _ticketDbAccess;
    private readonly UserDbAccess userDbAccess;

    private readonly ILogger<SalesOrderConductor> _logger;

    private readonly EmailUtils _emailUtils;

    private readonly EventItemTypeDbAccess _eventItemTypeDbAccess;
    private readonly StripeAccess _stripeAccess;
    private readonly EventDbAccess _eventDbAccess;

    private readonly EventOverrideBaseDbAccess _eventOverrideDbAccess;

    private readonly EventOrganizerDBAccess _eventOrganizerDbAccess;
    public SalesOrderConductor(ILogger<SalesOrderConductor> logger, SalesOrderDbAccess dbAccess, TicketAccess ticketAccess,
                    EventOrganizerDBAccess eventOrganizerDbAccess, UserDbAccess attendeeDbAccess, EventItemTypeDbAccess eventItemTypeDbAccess,
                    EmailUtils emailUtils, StripeAccess stripeAccess, EventDbAccess eventDbAccess, EventOverrideBaseDbAccess eventOverrideDbAccess)
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

        if (stripeAccess == null)
            throw new ArgumentNullException(nameof(_stripeAccess));
        if (eventOverrideDbAccess == null)
            throw new ArgumentNullException(nameof(eventOverrideDbAccess));

        _dbAccess = dbAccess;
        _ticketDbAccess = ticketAccess;
        _eventItemTypeDbAccess = eventItemTypeDbAccess;
        this.userDbAccess = attendeeDbAccess;
        _stripeAccess = stripeAccess;
        _emailUtils = emailUtils;
        _eventDbAccess = eventDbAccess;
        _eventOverrideDbAccess = eventOverrideDbAccess;
        _eventOrganizerDbAccess = eventOrganizerDbAccess;
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


    public async Task<(CustomerSalesOrder,bool,bool)> CreateSalesOrder(CustomerSalesOrder customerSalesOrder)
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
                string.IsNullOrWhiteSpace(customerSalesOrder.Name)) && customerSalesOrder.UserId <= 0)
            throw new ArgumentException("EmailAddress  and Name should be provided if there is no signed in Attendee", nameof(customerSalesOrder.EmailAddress));
        
        //Todo:Need to compare price of item from ui with price in db for eventitemtype table and warn user if there is a mismatch.
        //

        EventOrganizer organizer = await _eventOrganizerDbAccess.GetOrganizerById(customerSalesOrder.CustomerId);
        if (organizer == null)
            throw new Exception($"Invalid CustomerId {customerSalesOrder.CustomerId} sent");
   
        EventUser attendee;
        bool guestMode = false;
        bool guestAlreadyExists = false;
        if (customerSalesOrder.UserId <= 0 && !string.IsNullOrWhiteSpace(customerSalesOrder.EmailAddress))
        {
            guestMode = true;
            attendee = await userDbAccess.GetUserByEmail(customerSalesOrder.EmailAddress);
            if (attendee == null)
            {
                attendee = new EventUser
                {
                    Name = customerSalesOrder.Name,
                    Email = customerSalesOrder.EmailAddress,
                    Sms = string.Empty // Assuming SMS is not provided
                };
                customerSalesOrder.UserId = await userDbAccess.CreateUser(attendee);
                attendee.UserId = customerSalesOrder.UserId;
                _logger.LogInformation($"New attendee created with ID: {customerSalesOrder.UserId}");
            }
            else
            {
                customerSalesOrder.UserId = attendee.UserId;
                guestAlreadyExists = true;
                _logger.LogInformation($"Existing attendee found with ID: {attendee.UserId}");
            }
        }
        else
        {
            _logger.LogInformation($"Existing attendee provided: {customerSalesOrder.UserId}");
            attendee = await userDbAccess.GetUserById(customerSalesOrder.UserId);
            if (attendee == null)
            {
                _logger.LogCritical($"Unable to locate attendee provided: {customerSalesOrder.UserId}");
                throw new Exception("Unable to locate attendee provided");
            }
        }

        List<EventItemType> eventItemTypes = await _eventItemTypeDbAccess.GetAllEventItemTypesByEventId(customerSalesOrder.EventId);
        if (eventItemTypes == null || eventItemTypes.Count == 0)
            throw new Exception($"No EventItemTypes found for EventId {customerSalesOrder.EventId}");
        bool paymentRequired = customerSalesOrder.SalesOrderItems.Any(item => item.Cost > 0);

        if (paymentRequired && string.IsNullOrEmpty(customerSalesOrder.ZipCode))
        {
            throw new Exception("Zipcode is required for paid orders");
        }
        if (paymentRequired && string.IsNullOrEmpty(organizer.StripeAccountId))
        {
            throw new Exception("Organizer does not have a valid stripe account id");
        }
    
        if (paymentRequired && organizer.StripeConnectStatus != StripeAccountStatus.Completed.ToString())
        {
            throw new Exception("Organizer does not have a valid stripe connect status");
        }
        // Create the sales order
        var salesOrder = new SalesOrder
        {
            CustomerId = customerSalesOrder.CustomerId,
            EventId = customerSalesOrder.EventId,
            UserId = customerSalesOrder.UserId,
            DeliveryType = customerSalesOrder.DeliveryType,
            //generate sales order code only if payment is not required
            //otherwise will generate after payment is confirmed
            SalesOrderCode = !paymentRequired?PasswordGenerator.GetPassword():null, // Generate a unique sales order code
        };
        // Save the sales order to the database
        int orderId = await _dbAccess.CreateSalesOrder(salesOrder);
        salesOrder.OrderId = orderId;
        _logger.LogInformation($"Sales order created with ID: {orderId} and QRCode {salesOrder.SalesOrderCode}");
        if (orderId <= 0)
            throw new Exception("Failed to create sales order.");
        //Create event sales items

        List<ErrorResponseSalesOrderItems> errorItems = new List<ErrorResponseSalesOrderItems>();
        foreach (var item in customerSalesOrder.SalesOrderItems)
        {
            if (item.EventTicketTypeId <= 0)
                throw new ArgumentException("EventTicketTypeId cannot be null or empty.", nameof(item.EventTicketTypeId));
            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(item.Quantity));
            var eventItemType = eventItemTypes.FirstOrDefault(e => e.EventItemTypeId == item.EventTicketTypeId);
            if (eventItemType == null)
            {
                throw new Exception($"Invalid EventTicketTypeId {item.EventTicketTypeId} sent for EventId {customerSalesOrder.EventId}");
            }
            if (eventItemType.TotalAllowed < eventItemType.TicketsSold + item.Quantity)
            {
                errorItems.Add(new ErrorResponseSalesOrderItems
                {
                    EventItemTypeId = item.EventTicketTypeId,
                    PaymentRequired = item.Cost > 0,
                    Error = "Ticket are sold out for this item."
                });
                _logger.LogInformation($"Ticket are sold out for Item {eventItemType.Name} for EventId {customerSalesOrder.EventId}");
                break;
            }
            if (eventItemType.Cost != item.Cost)
            {
                errorItems.Add(new ErrorResponseSalesOrderItems
                {
                    EventItemTypeId = item.EventTicketTypeId,
                    PaymentRequired = item.Cost > 0,
                    Error = $"Price mismatch for EventTicketTypeId {item.EventTicketTypeId}. Expected: {eventItemType.Cost}, Received: {item.Cost}"
                });
                _logger.LogInformation($"Price mismatch for EventTicketTypeId {item.EventTicketTypeId}. Expected: {eventItemType.Cost}, Received: {item.Cost}");
                break;
            }
            _logger.LogInformation(@$"Prevalidation checks passed.
                                     Creating {item.Quantity} tickets for EventTicketTypeId {item.EventTicketTypeId} for SalesOrderId {orderId}");
            List<EventSalesItem> itemList = new List<EventSalesItem>();

            for (int i = 0; i < item.Quantity; i++)
            {
                var salesItem = new EventSalesItem
                {
                    SalesOrderId = orderId,
                    TicketStatus = !paymentRequired?TicketStatus.Live.ToString():null, // Assuming ticket is not scanned initially
                    EventId = customerSalesOrder.EventId,
                    User = attendee,
                    PricePaid = item.Cost,
                    EventItemType = new EventItemType
                    {
                        EventItemTypeId = item.EventTicketTypeId,
                    },
                    TicketCode = !paymentRequired?PasswordGenerator.GetPassword():null
                    // Generate a unique ticket code only if payment is not required otherwise
                    //wait until payment is confirmed
                };
                itemList.Add(salesItem);
            }
            int result = await _ticketDbAccess.AddEventTickets(itemList, customerSalesOrder.SimulationMode);
            if (result <0)
            {
                string error = result == -1 ? "Ticket are sold out for this item." : "An error occurred while creating ticket.";
                errorItems.Add(new  ErrorResponseSalesOrderItems
                            {
                                EventItemTypeId=  item.EventTicketTypeId,
                                PaymentRequired = item.Cost>0,
                                Error= error 
                            });
            }
            _logger.LogInformation($"result for {item.EventTicketTypeId} is {result}");
        }

        CustomerSalesOrder salesOrderReturn = new()
                {UserId = attendee.UserId, EmailAddress = attendee.Email, Name = attendee.Name, SalesOrderId = orderId};

        if (errorItems.Count >0)
        {
            _logger.LogInformation($"All ticket types failed to be added...Deleting sales order");
            await _dbAccess.DeleteSalesOrder(orderId);
        }
        else
        {
            if (!paymentRequired)
            {
                await _emailUtils.SendOrderConfirmationEmail(salesOrder, attendee);
                await _dbAccess.UpdateSalesOrderStatusAndStripeSessionId(orderId, SalesOrderStatus.OrderCompleted, string.Empty);
            }
            else
            {
               
                Event eventData = await _eventDbAccess.GetEventDetailsById(customerSalesOrder.EventId);   
                if (eventData == null)
                    throw new Exception("Invalid event Id sent");
                PaymentModel paymentModel = new PaymentModel
                {
                    EventId = eventData.EventId,
                    SalesOrderId = salesOrder.OrderId,
                    LocationId = eventData.LocationId,
                    EventCategory = eventData.Category
                };
                List<EventItemType> itemTypes = await _eventItemTypeDbAccess.GetAllEventItemTypesByEventId(salesOrder.EventId);
             
                List<PaymentLineItemModel> checkoutItems = [];
                //sending an item to stripe even if 0 cost since the some items are free and some are paid in the order.
                foreach (var item in customerSalesOrder.SalesOrderItems)
                {
                    checkoutItems?.Add(new PaymentLineItemModel()
                    {
                         EventTicketTypeId = item.EventTicketTypeId,
                         Price = item.Cost,
                         Quantity = item.Quantity,
                         Description =itemTypes?
                                        .FirstOrDefault(x => x.EventItemTypeId == item.EventTicketTypeId)?
                                        .Description ?? "No description available"
                    });
                    
                }
                paymentModel.LineItems = checkoutItems ?? new List<PaymentLineItemModel>();
                EventFeeOverride? overrideFees =  await _eventOverrideDbAccess.GetEventFeeOverride(eventData.EventId);
                Tuple<string,string,string> result = await _stripeAccess.CreateCheckoutSession(organizer.StripeAccountId,
                                                    paymentModel,
                                                    customerSalesOrder.EmailAddress, 
                                                    customerSalesOrder.ZipCode,
                                                    eventData.TicketFeeMode == TicketFeeMode.CustomerAbsorbsAll,
                                                    platformFeeOverride: overrideFees?.CustomPercentage,
                                                    floorFeesOverride: overrideFees?.CustomFloor,
                                                    simulationMode: customerSalesOrder.SimulationMode);
                salesOrderReturn.CheckoutSessionSecret = result.Item1;
                salesOrderReturn.CheckoutSessionId = result.Item2;
                salesOrderReturn.CheckoutSessionPublishableKey = result.Item3;
                await _dbAccess.UpdateSalesOrderStatusAndStripeSessionId(orderId, SalesOrderStatus.Reserved,result.Item2);
                _logger.LogInformation($"Sales order {orderId} checkout created with session id {result.Item2}");
            }
            if (!string.IsNullOrEmpty(salesOrder.SalesOrderCode))
            {
                salesOrderReturn.SalesOrderCode = salesOrder.SalesOrderCode;            
                salesOrderReturn.SalesOrderQrCodeImage = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(salesOrder.SalesOrderCode));      
            }
            
        }
        salesOrderReturn.SalesOrderItemsError = errorItems;
        return (salesOrderReturn,guestMode,guestAlreadyExists);
    }
    
    
}