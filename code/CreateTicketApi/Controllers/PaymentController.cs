using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using EventUtils;
using EventManagementDbAccess;
using CreateTicketApi.BusinessLogic;
// Ensure StripeAccess is in this namespace

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly StripeAccess _stripeAccess;
        private readonly IConfiguration _configuration;
  
        private readonly SalesOrderDbAccess _salesOrderDbAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDbAccess;
        private readonly EmailUtils _emailUtils;
        private readonly TicketAccess _ticketAccess;
        public PaymentController(
            ILogger<PaymentController> logger,
            StripeAccess stripeAccess,
            IConfiguration configuration,
            SalesOrderDbAccess salesOrderDbAccess,
            EventOrganizerDBAccess eventOrganizerDbAccess,
            EmailUtils emailUtils,
            TicketAccess ticketAccess)
        {
            _logger = logger;
            _stripeAccess = stripeAccess;
            _configuration = configuration;
            _salesOrderDbAccess = salesOrderDbAccess;
            _eventOrganizerDbAccess = eventOrganizerDbAccess;
            _ticketAccess = ticketAccess;
            _emailUtils = emailUtils ?? throw new ArgumentNullException(nameof(emailUtils), "EmailUtils cannot be null.");  
        }

        [HttpPost("create-account")]
        public async Task<IActionResult> CreateStripeAccount([FromBody]int customerId)
        {
            if (customerId <= 0)
            {
                return BadRequest("Invalid customer ID.");
            }

            try
            {
                EventOrganizer org = await _eventOrganizerDbAccess.GetOrganizerById(customerId);
                StripePrefillInfo info = new StripePrefillInfo()
                {
                    OrganizationName = org.OrganizationName,
                    OrganizerCountry = org.OrganizerCountry,
                    OrganizerDisplayName = org.OrganizationName,
                    OrganizerEmail = org.OrganizerEmail,
                    OrganizerPhone = org.OrganizerPhone,
                    OrganizerWebsite = org.OrganizerWebsite,
                };

                var result = await _stripeAccess.CreateStripeAccount(customerId,info);
                if (string.IsNullOrEmpty(result))
                {
                    return StatusCode(500, "Failed to create Stripe account.");
                }
                else
                {
                    await _eventOrganizerDbAccess.UpdateStripeAccountInfo(customerId, result, StripeAccountStatus.IdCreated);
                    _logger.LogInformation($"Stripe account created successfully for customer ID {customerId}.");
                    return Ok(result);
                }               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe account creation failed.");
                return StatusCode(500, "Stripe account creation failed.");
            }
        }

        [HttpGet("connect-status/{stripeAccountId}")]
        public async Task<IActionResult> GetStripeAccountConnectStatus(string stripeAccountId)
        {
             if (string.IsNullOrEmpty(stripeAccountId))
            {
                return BadRequest("Stripe account ID cannot be null or empty.");
            }
            try
            {
                return  Ok(await  _stripeAccess.IsAccountOnboarded(stripeAccountId));
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to get stripe account status for {stripeAccountId}");
                return StatusCode(500, $"Unable to get stripe account status for {stripeAccountId}");
            }
        }

        [HttpPost("initiate-account-link/{organizerId}")]
        public async Task<IActionResult> InitiateAccountLink(int organizerId, [FromBody]string stripeAcctId)
        {
            if (string.IsNullOrEmpty(stripeAcctId))
            {
                return BadRequest("Stripe account ID cannot be null or empty.");
            }
            if (organizerId <= 0)
            {
                return BadRequest("Invalid organizer ID.");
            }
            try
            {
                var result = await _stripeAccess.InitiateAccountLink(organizerId,stripeAcctId);
                if (string.IsNullOrEmpty(result))
                {
                    return StatusCode(500, "Failed to initiate account link.");
                }
                else
                {
                    _logger.LogInformation($"Stripe account link initiated successfully for account ID {stripeAcctId}.");
                    await _eventOrganizerDbAccess.UpdateStripeAccountInfo(organizerId, stripeAcctId, StripeAccountStatus.LinkInitiated);
                    return Ok(result);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe account creation failed.");
                return StatusCode(500, "Stripe account creation failed.");
            }
        }

        [HttpGet("checkout-session-status/{sessionId}/{stripAcctId}")]
        public async Task<string> GetCheckoutSessionStatus(string sessionId, string stripAcctId)
        {
            return await _stripeAccess.GetCheckOutSessionStatus(sessionId,stripAcctId);
        }


        [HttpPost("createcheckoutsession")]
        public async Task<IActionResult> CreateCheckoutSession(int salesOrderId, string stripeAccountId, int eventId,List<PaymentLineItemModel> request)
        {
            if (salesOrderId <= 0 || request == null || request.Count == 0)
            {
                return BadRequest("Invalid sales order ID or payment request.");
            }

            try
            {
                var result = await _stripeAccess.CreateCheckoutSession(salesOrderId, stripeAccountId,eventId, request);
                //todo: update session id , order status, in db
                if (result == null || string.IsNullOrEmpty(result.Item1) || string.IsNullOrEmpty(result.Item2))
                {
                    return StatusCode(500, "Payment processing failed.");
                }
                else
                {
                    _logger.LogInformation($"Payment session ID {result.Item2} created  successfully for sales order ID {salesOrderId}.");
                    // Update the sales order with the Stripe session ID
                    await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(salesOrderId, SalesOrderStatus.Reserved, result.Item2);
                    return Ok(result.Item1);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe charge failed.");
                return StatusCode(500, "Payment processing failed.");
            }
        }
        
        [HttpPost]
        [Route("/Payment/RefundOrder/{orderId}")]
        public async Task<IActionResult> RefundOrder(int orderId)
        {
            if (orderId <=0)
                return StatusCode(400,"Invalid order id sent");
            SalesOrder order =  await _salesOrderDbAccess.GetSalesOrderById(orderId);
            if (order == null)
            {
              return StatusCode(404,"Unable to find salesorder for id {orderId}");
            }
            if (order.SalesOrderStatus != SalesOrderStatus.PaymentSucceeded && order.SalesOrderStatus != SalesOrderStatus.RefundedPartially)
            {
                return StatusCode(409,"Order is in invalid state to start refund");
            }
            if (string.IsNullOrWhiteSpace(order.PaymentIntentId))
            {
                return StatusCode(409,"No paymentintentid found");
            }
            int total = await _ticketAccess.GetOrderTotalPrice(orderId);
            if (total == 0)
                return StatusCode(404,"Unable to start refund as total paid is 0");
            EventOrganizer organizer =await  _eventOrganizerDbAccess.GetOrganizerById(order.CustomerId);
            if (organizer == null)
                return StatusCode(404,"Unable to locate organizer to start refund.");
            var result = await _stripeAccess.RefundSalesOrder(total, orderId,order.PaymentIntentId,organizer.StripeAccountId );
            if (result.refundStatus)
            {
                return StatusCode(200,"Initiated refund successfully");
            }
            else
            {
                return StatusCode(500,"There was an issue initiating your refund");
            }
            
        }
        [HttpPost("stripewebhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation(json);
            try
            {
                var stripeEvent = StripeAccess.GetWebhookEventAndRefIdReceived(json, HttpContext.Request.Headers);
                if (stripeEvent == null)
                {                          
                    _logger.LogError("Invalid Stripe webhook event data.");
                    return BadRequest("Invalid Stripe webhook event data.");    
                }

                _logger.LogInformation($"event info is: {stripeEvent.ToString()}");

               // _logger.LogInformation($"Received Stripe webhook event: {stripeEvent.EventType} for SalesOrder ID: {stripeEvent.SalesOrderId}");
                // Handle the event
                if (stripeEvent.EventType.Contains("checkout.session.completed"))
                {
                    bool result =await _salesOrderDbAccess.UpdateSalesOrderStatus(
                                    stripeEvent.SalesOrderId,
                                    stripeEvent.PaymentSucceeded?  SalesOrderStatus.PaymentSucceeded : SalesOrderStatus.PaymentFailed
                                   );
                    if (!result)
                    {
                        _logger.LogError($"Failed to update sales order status for SalesOrder ID: {stripeEvent.SalesOrderId}");
                        return StatusCode(500,"Failed to update sales order status order id "+ stripeEvent.SalesOrderId); 
                    }
                    if (stripeEvent.PaymentSucceeded)
                    {
                        try
                        {
                        // Finalize the sales order. generate tickets etc
                        //Task.Delay(10000).Wait();
                        result = await _salesOrderDbAccess.FinalizeSalesOrder(stripeEvent.SalesOrderId, stripeEvent.SessionId,stripeEvent.PaymentIntentId);
                        if (!result)
                        {
                            _logger.LogError($"Failed to finalize sales order for SalesOrder ID: {stripeEvent.SalesOrderId}");
                            await _salesOrderDbAccess.UpdateSalesOrderStatus(
                                    stripeEvent.SalesOrderId,
                                    SalesOrderStatus.OrderFinalizationError
                                    );
                            //stripe will retry webhook for us with 500 error
                            return StatusCode(500, "Failed to finalize sales order for order id " + stripeEvent.SalesOrderId);
                        }   
                        _logger.LogInformation($"Sales order {stripeEvent.SalesOrderId} finalized successfully.");
                        // Send confirmation email to customer
                        //await _emailUtils.SendOrderConfirmationEmail();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error in checkoutsession completed {0} for order id {1}", ex,stripeEvent.SalesOrderId );
                            return StatusCode(500,$"Error in checkoutsession completed {ex} for order id {stripeEvent.SalesOrderId}");
                        }
                    }
                    
                    // Process the completed checkout session (e.g., update order status)
                   _logger.LogInformation($"Checkout Session Completed for SalesOrder: {stripeEvent.SalesOrderId} with status {(stripeEvent.PaymentSucceeded? "PaymentSucceeded":"PaymentFailed")} ");
                }
                // Handle other event types as needed       
                else if (stripeEvent.EventType.Contains("async_payment_failed"))
                {
                }
                // Handle other event types as needed       
                else if (stripeEvent.EventType.Contains("refund.created"))
                {
                    SalesOrderStatus tempStatus = stripeEvent.RefundStatus=="succeeded"?
                                                    SalesOrderStatus.RefundSuccess
                                                    :SalesOrderStatus.RefundFailed;
                    _logger.LogInformation($"refund.created received for order id {stripeEvent.SalesOrderId} with refundid {stripeEvent.RefundId} and status {stripeEvent.RefundStatus}");
                       
                    if (tempStatus == SalesOrderStatus.RefundSuccess)
                    {
                       bool ret = await _salesOrderDbAccess.ReturnTicketsToPool(tempStatus, "",stripeEvent.SalesOrderId);
                       if (ret)
                       {
                            _logger.LogInformation($"Returned tickets to pool status for {stripeEvent.SalesOrderId} in db is success");

                            ret =await _salesOrderDbAccess.UpdateSalesOrderRefundStatus(stripeEvent.SalesOrderId, tempStatus,
                                    stripeEvent.RefundId, (int)stripeEvent.RefundAmount);
                            _logger.LogInformation($"Refund update  for SalesOrder: {stripeEvent.SalesOrderId} in db is {ret}");
                       }
                       if (!ret)
                        {
                            _logger.LogError(@$"Failed to update refund status or return tickets to pool
                                         for SalesOrder ID: {stripeEvent.SalesOrderId}");
                            await _salesOrderDbAccess.UpdateSalesOrderStatus(
                                    stripeEvent.SalesOrderId,
                                    SalesOrderStatus.RefundUpdateDbError
                                  );
                            //stripe will retry webhook for us with 500 error
                            return StatusCode(500, "Failed to finalize sales order for order id " + stripeEvent.SalesOrderId);
                        }            
                    }
                    else
                    {
                        await _salesOrderDbAccess.UpdateSalesOrderStatus(
                                stripeEvent.SalesOrderId,
                                tempStatus);
                    }
                }
                else if (stripeEvent.EventType.Contains("account.updated"))
                {
                    StripeAccountStatus status = stripeEvent.DetailsSubmitted ? StripeAccountStatus.Completed:
                                                (stripeEvent.RequirementsPending ? StripeAccountStatus.RequirementsPending: 
                                                StripeAccountStatus.InProgress);
                    _logger.LogInformation(@$"updating stripe account: {stripeEvent.AccountId} for 
                                        event customer id {stripeEvent.CustomerId} to status {status}");     
                    bool result = await _eventOrganizerDbAccess.UpdateStripeStatus(stripeEvent.CustomerId, stripeEvent.AccountId, status);
                    _logger.LogInformation($"Result of account status updating for customerid {stripeEvent.CustomerId} is {result}");
                }
                else
                {
                    Console.WriteLine($"Unhandled event type: {stripeEvent.EventType}");
                }

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError($"error in webhook {e}");
                return  StatusCode(500,$"Error in processing {e}");
            }
        }
    }
}