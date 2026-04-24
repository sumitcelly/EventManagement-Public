using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using EventUtils;
using EventManagementDbAccess;
using CreateTicketApi.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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

        private readonly EventDbAccess _eventDbAccess;
        private readonly EmailUtils _emailUtils;
        private readonly TicketAccess _ticketAccess;
        public PaymentController(
            ILogger<PaymentController> logger,
            StripeAccess stripeAccess,
            IConfiguration configuration,
            SalesOrderDbAccess salesOrderDbAccess,
            EventOrganizerDBAccess eventOrganizerDbAccess,
            EmailUtils emailUtils,
            TicketAccess ticketAccess,
            EventDbAccess eventDbAccess)
        {
            _logger = logger;
            _stripeAccess = stripeAccess;
            _configuration = configuration;
            _salesOrderDbAccess = salesOrderDbAccess;
            _eventOrganizerDbAccess = eventOrganizerDbAccess;
            _ticketAccess = ticketAccess;
            _eventDbAccess = eventDbAccess;
            _emailUtils = emailUtils ?? throw new ArgumentNullException(nameof(emailUtils), "EmailUtils cannot be null.");  
        }

        [HttpPost("create-account/{customerId}")]
        [Authorize(Policy = "OwnerOnly")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> CreateStripeAccount(int customerId)
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

        [HttpGet("transactionfees")]
        public async Task<IActionResult> GetTransactionFees()
        {
            return Ok( new
            {
                PlatformFees= _configuration["Fees:Platform"],
                StripeFees = _configuration["Fees:Stripe"],
                StripeFixed =_configuration["Fees:StripeFixed"],
            }) ;
        }

        [HttpGet("connect-status/{stripeAccountId}")]
        [Authorize(Policy = "FullAdminMinimum")]
        public async Task<IActionResult> GetStripeAccountConnectStatus(string stripeAccountId)
        {
            if (string.IsNullOrEmpty(stripeAccountId))
            {
                return BadRequest("Stripe account ID cannot be null or empty.");
            }
            try
            {
                int customerId = await _eventOrganizerDbAccess.GetOrganizerIdByStripeAccountId(stripeAccountId); // just to check if the stripe account id is valid and belongs to an organizer in our system
                string userCustomerId = User.FindFirst("CustomerId")?.Value.ToString() ?? "";
                if (customerId <= 0 || customerId.ToString() != userCustomerId)
                {
                    return BadRequest("Invalid Stripe account ID.");
                }

                return  Ok(await  _stripeAccess.IsAccountOnboarded(stripeAccountId));
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to get stripe account status for {stripeAccountId}");
                return StatusCode(500, $"Unable to get stripe account status for {stripeAccountId}");
            }
        }

        [HttpPost("initiate-account-link/{customerId}")]
        [Authorize(Policy = "OwnerOnly")]
        [Authorize(Policy ="MatchingCustomer")]
        public async Task<IActionResult> InitiateAccountLink(int customerId, [FromBody]string stripeAcctId)
        {
            if (string.IsNullOrEmpty(stripeAcctId))
            {
                return BadRequest("Stripe account ID cannot be null or empty.");
            }
            if (customerId <= 0)
            {
                return BadRequest("Invalid organizer ID.");
            }
            try
            {
                var result = await _stripeAccess.InitiateAccountLink(customerId,stripeAcctId);
                if (string.IsNullOrEmpty(result))
                {
                    return StatusCode(500, "Failed to initiate account link.");
                }
                else
                {
                    _logger.LogInformation($"Stripe account link initiated successfully for account ID {stripeAcctId}.");
                    await _eventOrganizerDbAccess.UpdateStripeAccountInfo(customerId, stripeAcctId, StripeAccountStatus.LinkInitiated);
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
        [Authorize]
        public async Task<IActionResult> GetCheckoutSessionStatus(string sessionId, string stripAcctId)
        {
            //validate the  stripe session id belongs to the logged in user.
            var tempOrder = await _salesOrderDbAccess.GetSalesOrderByStripeSessionId(sessionId);
            if (tempOrder == null)
            {
                return BadRequest("Invalid session id.");
            }
            
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            if (tempOrder.UserId <= 0 || tempOrder.UserId.ToString() != userId)
            {
                return Forbid("Invalid session id");
            }
            return Ok(await _stripeAccess.GetCheckOutSessionStatus(sessionId,stripAcctId));
        }

        //This controller method is not called currently.
        [HttpPost("createcheckoutsession")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession(int salesOrderId, string stripeAccountId, int eventId,List<PaymentLineItemModel> request)
        {
            if (salesOrderId <= 0 || request == null || request.Count == 0)
            {
                return BadRequest("Invalid sales order ID or payment request.");
            }

            return Ok("This endpoint is not used currently. Please use the checkout session created in SalesOrderConductor to start the payment process.");

            // try
            // {
            //     var result = await _stripeAccess.CreateCheckoutSession(salesOrderId, stripeAccountId,eventId, request);
            //     //todo: update session id , order status, in db
            //     if (result == null || string.IsNullOrEmpty(result.Item1) || string.IsNullOrEmpty(result.Item2))
            //     {
            //         return StatusCode(500, "Payment processing failed.");
            //     }
            //     else
            //     {
            //         _logger.LogInformation($"Payment session ID {result.Item2} created  successfully for sales order ID {salesOrderId}.");
            //         // Update the sales order with the Stripe session ID
            //         await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(salesOrderId, SalesOrderStatus.Reserved, result.Item2);
            //         return Ok(result.Item1);
            //     }
                
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Stripe charge failed.");
            //     return StatusCode(500, "Payment processing failed.");
            // }
        }
        
        [HttpPost]
        [Route("/Payment/RefundOrder/{orderId}")]
        [Authorize(Policy = "OrderOwnedByUser")]
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
            EventHeader evt = await _eventDbAccess.GetEventHeaderById(order.EventId);
            if (evt == null || evt.RefundMode != RefundMode.CustomerControlled)
            {
                return StatusCode(409,"Event does not allow customer initiated refunds");
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
                    _logger.LogInformation($@"Received checkout completed for order {stripeEvent.SalesOrderId} with 
                                            status of {stripeEvent.PaymentSucceeded}")  ;
                   
                    if (stripeEvent.PaymentSucceeded)
                    {
                        try
                        {
                            // Finalize the sales order. generate tickets etc
                            //Task.Delay(10000).Wait();
                            bool result = await _salesOrderDbAccess.FinalizeSalesOrder(stripeEvent.SalesOrderId, 
                                                                                stripeEvent.SessionId,
                                                                                stripeEvent.PaymentIntentId,
                                                                                stripeEvent.OrderTotal,
                                                                                stripeEvent.PlatformFees,
                                                                                stripeEvent.TotalFeesForTransaction,
                                                                                stripeEvent.TotalTax
                                                                               );
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
                            await _emailUtils.SendOrderConfirmationEmail(null, null,stripeEvent.SalesOrderId,
                                                                        (stripeEvent.OrderTotal/100.0m).ToString("C"), stripeEvent.CustomerEmail);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error in checkoutsession completed {0} for order id {1}", ex,stripeEvent.SalesOrderId );
                            return StatusCode(500,$"Error in checkoutsession completed {ex} for order id {stripeEvent.SalesOrderId}");
                        }
                    }
                    else
                    {
                         await _salesOrderDbAccess.UpdateSalesOrderStatus(
                                        stripeEvent.SalesOrderId,
                                        SalesOrderStatus.PaymentFailed
                                        );
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
                       bool ret = await _salesOrderDbAccess.ReturnTicketsToPool(tempStatus, "",0,stripeEvent.SalesOrderId);
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
                    StripeAccountStatus status = stripeEvent.ChargedEnabled ? StripeAccountStatus.Completed:
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