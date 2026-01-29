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
    public SalesOrderConductor(ILogger<SalesOrderConductor> logger, SalesOrderDbAccess dbAccess, TicketAccess ticketAccess,
                    EventOrganizerDBAccess eventOrganizerDbAccess, UserDbAccess attendeeDbAccess, EventItemTypeDbAccess eventItemTypeDbAccess,
                    EmailUtils emailUtils, StripeAccess stripeAccess)
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

        _dbAccess = dbAccess;
        _ticketDbAccess = ticketAccess;
        _eventItemTypeDbAccess = eventItemTypeDbAccess;
        this.userDbAccess = attendeeDbAccess;
        _stripeAccess = stripeAccess;
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

    public async Task<CustomerSalesOrder> UpdateSalesOrder(int salesOrderId, CustomerSalesOrder order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));
        if (salesOrderId <= 0)
            throw new ArgumentException("OrderId cannot be null or empty.", nameof(salesOrderId));

        // Update the sales order
        // var previousTicketCount = await _ticketDbAccess.GetEventTicketCountBySalesOrderId(salesOrderId);
        // if (previousTicketCount == 0)
        //     throw new Exception($"Failed to retrieve tickts for previous order with ID {salesOrderId}.");

        //Todo: ORder count can be same if they changed the type of ticket but not the count
        // int newTicketCount = order.SalesOrderItems.Count();
        // if (previousTicketCount == newTicketCount)
        //     throw new Exception("Nothing to update since number of tickets in new and existing order are same.");

        bool result = await _dbAccess.DeleteSalesOrder(salesOrderId);
        if (!result)
        {
            throw new Exception($"Failed to delete sales order with ID {salesOrderId}. Cannot proceed with update.");
        }
        _logger.LogInformation($"Sales order {salesOrderId} deleted successfully. Proceeding to create new order with updated ticket count.");
        // Create a new sales order with updated ticket count
        return await CreateSalesOrder(order);      
    }

    public async Task<IEnumerable<EventSalesItem>> GetSalesOrderByQrCode(int id,string salesOrderQrCode)
    {
        if (id <= 0)
            throw new ArgumentException("Event id cannot be null or empty.", nameof(id));

        // Fetch the event sales items associated with the sales order
        IEnumerable<EventSalesItem> eventSalesItems = await _ticketDbAccess.GetEventTicketBasicsBySalesOrderQrCodeFromDb(salesOrderQrCode,id);
        if (eventSalesItems == null || !eventSalesItems.Any())
            throw new Exception($"No event sales items found for sales order ID {salesOrderQrCode}.");
        eventSalesItems.ToList().ForEach(item =>
        {
            //item.SalesOrderId = salesOrder.OrderId;
            item.QRBase64Image = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(item.TicketCode));
            item.TicketCode = item.TicketCode;
            item.TicketStatus = item.TicketStatus;
            item.EventItemType.Name = item.EventItemType.Name;
            item.EventItemType.EventItemTypeId = item.EventItemType.EventItemTypeId;
             // Clear the ticket code for security reasons
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
                string.IsNullOrWhiteSpace(customerSalesOrder.Name)) && customerSalesOrder.UserId <= 0)
            throw new ArgumentException("EmailAddress  and Name should be provided if there is no signed in Attendee", nameof(customerSalesOrder.EmailAddress));
        
        //Todo:Need to compare price of item from ui with price in db for eventitemtype table and warn user if there is a mismatch.
        //

        EventUser attendee;
        if (customerSalesOrder.UserId <= 0 && !string.IsNullOrWhiteSpace(customerSalesOrder.EmailAddress))
        {
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
                _logger.LogInformation($"Existing attendee found with ID: {attendee.UserId}");
            }
        }
        else
        {
            _logger.LogInformation($"Existing attendee provided: {customerSalesOrder.UserId}");
            attendee = await userDbAccess.GetUserById(customerSalesOrder.UserId);
        }

        bool paymentRequired = customerSalesOrder.SalesOrderItems.Any(item => item.Cost > 0);
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
                    TicketCode = !paymentRequired?EventUtils.PasswordGenerator.GetPassword():null
                    // Generate a unique ticket code only if payment is not required otherwise
                    //wait until payment is confirmed
                };
                itemList.Add(salesItem);
            }
            int result = await _ticketDbAccess.AddEventTickets(itemList);
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

        CustomerSalesOrder salesOrderReturn = new();

        if (errorItems.Count >0)
        {
            _logger.LogInformation($"All ticket types failed to be added...Deleting sales order");
            await _dbAccess.DeleteSalesOrder(orderId);
        }
        else
        {
            if (!customerSalesOrder.PaymentRequired)
            {
                await _emailUtils.SendOrderConfirmationEmail(salesOrder, attendee);
                await _dbAccess.UpdateSalesOrderStatusAndStripeSessionId(orderId, SalesOrderStatus.OrderCompleted, string.Empty);
            }
            else
            {
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
                Tuple<string,string> result = await _stripeAccess.CreateCheckoutSession(orderId, customerSalesOrder.StripeConnectedAccountId,customerSalesOrder.EventId, checkoutItems, customerSalesOrder.EmailAddress);
                salesOrderReturn.CheckoutSessionSecret = result.Item1;
                salesOrderReturn.CheckoutSessionId = result.Item2;
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
        return salesOrderReturn;
    }
    
    
}