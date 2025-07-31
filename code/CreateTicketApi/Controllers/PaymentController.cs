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
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly StripeAccess _stripeAccess;
        private readonly IConfiguration _configuration;
  
        private readonly SalesOrderDbAccess _salesOrderDbAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDbAccess;
        private readonly EmailUtils _emailUtils;

        public PaymentController(
            ILogger<PaymentController> logger,
            StripeAccess stripeAccess,
            IConfiguration configuration,
            SalesOrderDbAccess salesOrderDbAccess,
            EventOrganizerDBAccess eventOrganizerDbAccess,
            EmailUtils emailUtils)
        {
            _logger = logger;
            _stripeAccess = stripeAccess;
            _configuration = configuration;
            _salesOrderDbAccess = salesOrderDbAccess;
            _eventOrganizerDbAccess = eventOrganizerDbAccess;
            _emailUtils = emailUtils ?? throw new ArgumentNullException(nameof(emailUtils), "EmailUtils cannot be null.");  
        }

        [HttpPost("create-account")]
        public async Task<IActionResult> CreateStripeAccount(int customerId)
        {
            if (customerId <= 0)
            {
                return BadRequest("Invalid customer ID.");
            }

            try
            {
                var result = await _stripeAccess.CreateStripeAccount(customerId);
                if (string.IsNullOrEmpty(result))
                {
                    return StatusCode(500, "Failed to create Stripe account.");
                }
                else
                {
                    await _eventOrganizerDbAccess.UpdateStripeAccountInfo(customerId, result, StripeAccountStatus.IdCreated);
                    _logger.LogInformation($"Stripe account created successfully for customer ID {customerId}.");
                    return Ok(new { StripeAccountId = result });
                }               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe account creation failed.");
                return StatusCode(500, "Stripe account creation failed.");
            }
        }

        [HttpPost("initiate-account-link")]
        public async Task<IActionResult> InitiateAccountLink(int organizerId, string stripeAcctId)
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
                var result = await _stripeAccess.InitiateAccountLink(stripeAcctId);
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

        [HttpPost("charge")]
        public async Task<IActionResult> Charge(int salesOrderId, string stripeAccountId, List<PaymentLineItemModel> request)
        {
            if (salesOrderId <= 0 || request == null || request.Count == 0)
            {
                return BadRequest("Invalid sales order ID or payment request.");
            }

            try
            {
                var result = await _stripeAccess.BuySalesItem(salesOrderId, stripeAccountId, request);
                //todo: update session id , order status, in db
                if (result == null || string.IsNullOrEmpty(result.Item1) || string.IsNullOrEmpty(result.Item2))
                {
                    return StatusCode(500, "Payment processing failed.");
                }
                else
                {
                    _logger.LogInformation($"Payment session ID {result.Item2} created  successfully for sales order ID {salesOrderId}.");
                    // Update the sales order with the Stripe session ID
                    await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(salesOrderId, SalesOrderStatus.PaymentPending, result.Item2);
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
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = StripeAccess.GetWebhookEventAndRefIdReceived(json, HttpContext.Request.Headers);
                if (stripeEvent == null || stripeEvent.SalesOrderId <= 0)
                {
                    _logger.LogError("Invalid Stripe webhook event data.");
                    return BadRequest("Invalid Stripe webhook event data.");    
                }

                _logger.LogInformation($"Received Stripe webhook event: {stripeEvent.EventType} for SalesOrder ID: {stripeEvent.SalesOrderId}");
                // Handle the event
                if (stripeEvent.EventType.Contains("CheckoutSessionCompleted"))
                {
                    await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(stripeEvent.SalesOrderId, SalesOrderStatus.PaymentInitiated, stripeEvent.SessionId);
                    // Process the completed checkout session (e.g., update order status)
                   _logger.LogInformation($"Checkout Session Completed for SalesOrder: {stripeEvent.SalesOrderId}");
                }
                // Handle other event types as needed
                else if (stripeEvent.EventType.Contains("PaymentSucceeded"))
                {
                    //update  order status, in db
                    //send email?   
                    SalesOrder order = await _salesOrderDbAccess.GetSalesOrderById(stripeEvent.SalesOrderId);
                    if (order == null)
                    {
                        _logger.LogError($"SalesOrder with ID {stripeEvent.SalesOrderId} not found.");
                        return NotFound($"SalesOrder with ID {stripeEvent.SalesOrderId} not found.");
                    }
                    await  _emailUtils.SendOrderConfirmationEmail(order);
                    await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(stripeEvent.SalesOrderId, SalesOrderStatus.OrderCompleted, stripeEvent.SessionId);
                    
                    _logger.LogInformation($"Payment succeeded for SalesOrder: {stripeEvent.SalesOrderId}");
                }
                else if (stripeEvent.EventType.Contains("PaymentFailed"))
                {
                    await _salesOrderDbAccess.UpdateSalesOrderStatusAndStripeSessionId(stripeEvent.SalesOrderId, SalesOrderStatus.PaymentFailed, stripeEvent.SessionId);
   
                    _logger.LogError($"Payment failed for SalesOrder: {stripeEvent.SalesOrderId}");
                }
                else
                {
                    Console.WriteLine($"Unhandled event type: {stripeEvent.EventType}");
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}