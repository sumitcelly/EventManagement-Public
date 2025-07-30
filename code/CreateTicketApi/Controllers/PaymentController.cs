using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using EventUtils; // Ensure StripeAccess is in this namespace

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly StripeAccess _stripeAccess;
        private readonly IConfiguration _configuration;
  

        public PaymentController(
            ILogger<PaymentController> logger,
            StripeAccess stripeAccess,
            IConfiguration configuration)
        {
            _logger = logger;
            _stripeAccess = stripeAccess;
            _configuration = configuration;
           
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe account creation failed.");
                return StatusCode(500, "Stripe account creation failed.");
            }
        }

        [HttpPost("initiate-account-link")]
        public async Task<IActionResult> InitiateAccountLink(string stripeAcctId)
        {
            if (string.IsNullOrEmpty(stripeAcctId))
            {
                return BadRequest("Stripe account ID cannot be null or empty.");
            }

            try
            {
                var result = await _stripeAccess.InitiateAccountLink(stripeAcctId);
                return Ok(result);
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
                return Ok(result);
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

            // Handle the event
            if (stripeEvent.Item1.Contains("CheckoutSessionCompleted"))
            {
                
                // Process the completed checkout session (e.g., update order status)
                Console.WriteLine($"Checkout Session Completed for SalesOrder: {stripeEvent.Item2}");
            }
            // Handle other event types as needed
            else if (stripeEvent.Item1.Contains("PaymentSucceeded"))
            {
                //update  order status, in db
                //send email?
                Console.WriteLine($"Payment succeeded for SalesOrder: {stripeEvent.Item2}");
            }
            else if (stripeEvent.Item1.Contains("PaymentFailed"))
            {
                Console.WriteLine($"Payment failed for SalesOrder: {stripeEvent.Item2}");
            }
            else
            {
                Console.WriteLine($"Unhandled event type: {stripeEvent.Item1}");
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