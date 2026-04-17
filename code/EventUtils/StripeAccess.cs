using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Stripe;
using Microsoft.Extensions.Logging;

using Amazon.Runtime.Internal.Util;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.Extensions.Primitives;
using Stripe.Checkout;
namespace EventUtils;

public class StripeAccess
{
    private readonly Microsoft.Extensions.Logging.ILogger<StripeAccess> _logger;
    private readonly string _connectReturnUrl = "http://localhost:5173/organizermanager/customerId/stripe";
    private readonly string _connectRefreshUrl = "http://localhost:5173/organizermanager/customerId/stripe";
    private readonly string _paymentReturnUrl = "http://localhost:5173/orderpayment/event/event_id?salesOrderId=order_Id&session_id={CHECKOUT_SESSION_ID}";

    private readonly decimal _applicationFeePercentage = 0.03m; // Example: 10% application fee

    private readonly decimal _fixedTransactionFee = 1.0m; // Example: $0.30 fixed fee per transaction

    private static string WebhookSecret { get; set; }


    public StripeAccess(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger<StripeAccess> logger)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        }

        _logger = logger;

        if (string.IsNullOrWhiteSpace(configuration["Stripe:SecretKey"]))
        {
            throw new ArgumentException("Stripe Secret key is not configured.");
        }

        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        WebhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(WebhookSecret))
        {
            throw new ArgumentException("Stripe webhook secret is not configured.");
        }
       // _applicationFeePercentage = Convert.ToDecimal(configuration["Stripe:ApplicationFeePercentage"]);
        logger.LogInformation("Initializing Stripe API with provided configuration.");
    }

    public async Task<string> CreateStripeAccount(int customerId,StripePrefillInfo stripePrefillInfo)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Stripe customer ID cannot be null or empty.", nameof(customerId));
        }

        _logger.LogInformation($"Connecting to Stripe customer: {customerId}");
        try
        {
            var service = new AccountService();
            
            var options = new AccountCreateOptions();
            /*Stripe collects fees directly from your connected account. We don’t charge any Connect fees to it or to your platform.

                Any application fees that your platform bills to the connected account are in addition to Stripe fees.

                You can set the fee payer when you create connected accounts. Accounts created with type=standard also have this value.*/
            options.Type = "standard";
            options.Email = stripePrefillInfo.OrganizerEmail;
            options.Country = stripePrefillInfo.OrganizerCountry;
            options.BusinessProfile = new AccountBusinessProfileOptions()
            {
                Name = stripePrefillInfo.OrganizationName,
                Url = stripePrefillInfo.OrganizerWebsite
            };
            options.Capabilities = new AccountCapabilitiesOptions()
            {
                 CardPayments = new AccountCapabilitiesCardPaymentsOptions (){ Requested =true},
                 Transfers = new AccountCapabilitiesTransfersOptions (){ Requested =true}
            };
            
            options.Metadata = new Dictionary<string, string>
            {
                { "CustomerId", customerId.ToString() }
            };


            Account account = await service.CreateAsync(options);

            return account.Id;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred when calling the Stripe API to create an account:  " + ex.Message);
            throw new InvalidOperationException("Failed to connect to Stripe account.", ex);
        }

    }
    public async Task<bool> IsAccountOnboarded(string accountId)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentException("Stripe account ID cannot be null or empty.", nameof(accountId));
        }

        _logger.LogInformation($"Initiating account link for Stripe account: {accountId}");
        try
        {
            var service = new AccountService();
            Account acct = service.Get(accountId);
            if (acct != null)
            {
               return acct.DetailsSubmitted;
            }
            else
            {
                _logger.LogCritical($"An error occurred when calling the Stripe API to get account status: no acct object returned for acctid {accountId}" );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred when calling the Stripe API to get account status:  " + ex.Message);
            throw new InvalidOperationException("Failed to get account status.", ex);
        }
    }
    public async Task<string> InitiateAccountLink(int organizerId,string accountId)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentException("Stripe account ID cannot be null or empty.", nameof(accountId));
        }

        _logger.LogInformation($"Initiating account link for Stripe account: {accountId}");
        try
        {
            var service = new AccountLinkService();

            var options = new AccountLinkCreateOptions
            {
                Account = accountId,
                //
                RefreshUrl = _connectRefreshUrl.Replace("customerId",organizerId.ToString()),
                //the url to redirect the client to after they complete the account onboarding process
                ReturnUrl = _connectReturnUrl.Replace("customerId",organizerId.ToString()),
                Type = "account_onboarding",
                //collect incrementally, not upfront
                CollectionOptions = new AccountLinkCollectionOptionsOptions(){ Fields ="currently_due"}

            };

            AccountLink accountLink = await service.CreateAsync(options);
            
            //The url to redirect the client to complete the account onboarding process
            return accountLink.Url;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("An error occurred when calling the Stripe API to initiate an account link:  " + ex.Message);
            throw new InvalidOperationException("Failed to initiate Stripe account link.", ex);
        }
    }

    private long CalculatePlatformFee(List<PaymentLineItemModel> lineItems)
    {
        decimal totalAmount = 0;
        foreach (var item in lineItems)
        {
            totalAmount += item.Price * item.Quantity;
        }
        long i = Convert.ToInt64(totalAmount * _applicationFeePercentage *100); // Example: 10% application fee
        _logger.LogInformation($"application fees amount is {i} using fees percent {_applicationFeePercentage}.");
        return i;
    }

    public async Task<string> GetCheckOutSessionStatus(string sessionId, string stripeAccountID)
    {
        if (string.IsNullOrEmpty(stripeAccountID))
        {
            throw new ArgumentException("Stripe customer ID cannot be null or empty.", nameof(stripeAccountID));
        }
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));
        }

        var requestOptions = new RequestOptions
        {
            StripeAccount = stripeAccountID,

        };
        var service = new Stripe.Checkout.SessionService();
        Stripe.Checkout.Session session = await service.GetAsync(sessionId, null, requestOptions);
        if (session == null)
        {
            throw new InvalidOperationException($"No session found for session ID: {sessionId}");
        }
        else
        {
            _logger.LogInformation($"Session details: ID={session.Id}, Status={session.Status}, PaymentStatus={session.PaymentStatus}");
            return session.PaymentStatus;
        }
        
    }

    /// <summary>
    /// Refunds the amount in cents for sales order id. The amount is expected to be the total price of the ticket(s), 
    /// no fees included since fees are not refunded.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="salesOrderId"></param>
    /// <param name="paymentIntentId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<(bool refundStatus, string refundId, bool isPartial)> RefundSalesOrder(int amount, int salesOrderId,string paymentIntentId, string stripAcctId)
    {
        if (amount <=0 || string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogError($"Cannot process refund if amount {amount} is 0 or payment intent id {paymentIntentId} is empty ");
            throw new ArgumentException("Either amount or payment intentId is invalid");
        }
        if (string.IsNullOrWhiteSpace(stripAcctId))
        {
            throw new ArgumentNullException(stripAcctId, nameof(stripAcctId));
        }
        try
        {
            RefundService _refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amount,
                // Amount is optional here; Stripe refunds the full remaining amount by default
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    { "SalesOrderId", salesOrderId.ToString() },
                   
                }
            };  

            var requestOptions = new RequestOptions
            {
                StripeAccount = stripAcctId// The organizer's Connect account ID
            }; 
            Refund refund =  await _refundService.CreateAsync(options,requestOptions);
           
            _logger.LogInformation($"Status of refund for order id {salesOrderId} is {refund.Status}");
            _logger.LogInformation($"Refund object for sales order id {salesOrderId} is {refund.ToJson()}");
            if (refund.Amount < amount)
            {
                _logger.LogCritical(@$"Amount refunded is less than requested for order id {salesOrderId}. 
                    Request is {amount} and refunded is {refund.Amount}");
            }
            return (refund.Status == "succeeded", refund.Id, refund.Amount < amount);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Exception when processing refund {ex}");
            throw;
        }

    }
    
    /// <summary>
    /// Processes the purchase of sales items using Stripe Checkout.Initiates a Stripe Checkout session.
    /// 
    /// </summary>
    /// <param name="salesOrderId"></param>
    /// <param name="stripeAccountID"></param>
    /// <param name="lineItems"></param>
    /// <returns>Tuple containing client secret to be used by UI (Item1) and
    /// SessionId (item2)</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Tuple<string, string>> CreateCheckoutSession(int salesOrderId, string stripeAccountID, int eventId,
                                            List<PaymentLineItemModel> lineItems,
                                             string customerEmailAddress="",
                                             bool passOnAllFeesToCustomer = false)
    {
        if (string.IsNullOrEmpty(stripeAccountID))
        {
            throw new ArgumentException("Stripe customer ID cannot be null or empty.", nameof(stripeAccountID));
        }
        if (lineItems == null || lineItems.Count == 0)
        {
            throw new ArgumentException("Line items cannot be null or empty.", nameof(lineItems));
        }

        long appFees = CalculatePlatformFee(lineItems);
        long totalItemsUnitPrice = (long)lineItems.Sum(item => item.Price * item.Quantity * 100);
        _logger.LogInformation($"Total items price in cents: {totalItemsUnitPrice}");
        long finalTotal=0, totalFeesForTrans=0;
        if (passOnAllFeesToCustomer)
        {
           (finalTotal, totalFeesForTrans) = StripeFeeCalculator.Calculate(totalItemsUnitPrice, appFees);
        }
        //absorb the stripe fees but pass on the application fees
        else
        {
            finalTotal = totalItemsUnitPrice + appFees;
            totalFeesForTrans = appFees;
        }
        
        _logger.LogInformation($"Fees after stripe calculation is {finalTotal} {totalFeesForTrans}");

        _logger.LogInformation($"Processing purchase for customer: {stripeAccountID} with {lineItems.Count} line items.");
        var options = new SessionCreateOptions
        {
            ReturnUrl = _paymentReturnUrl.Replace("event_id", eventId.ToString()).Replace("order_Id", salesOrderId.ToString()),
            PaymentMethodTypes = new List<string>
            {
              "card"
            
            },
            Metadata = new Dictionary<string, string>
            {
                { "SalesOrderId", salesOrderId.ToString() },
                { "EventId", eventId.ToString() },
                { "PlatformFees", appFees.ToString() },
                { "TotalFeesForTransaction", totalFeesForTrans.ToString() },
                { "PassOnAllFeesToCustomer", passOnAllFeesToCustomer.ToString() }
            },
            PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = appFees,
            },
            //one time payment
            Mode = "payment",
            UiMode = "embedded"

        };
        options.ClientReferenceId = salesOrderId.ToString();
        options.LineItems = new List<SessionLineItemOptions>();
        foreach (var item in lineItems)
        {
            options.LineItems.Add(new Stripe.Checkout.SessionLineItemOptions
            {
                PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Description,
                    },
                    UnitAmount = (long)(item.Price * 100), // Convert to cents
                },
                Quantity = item.Quantity,

            });
        }

        options.LineItems.Add(new SessionLineItemOptions {
            PriceData = new SessionLineItemPriceDataOptions {
                UnitAmount = finalTotal - totalItemsUnitPrice, // The remaining "Service Fee" (approx $1.95)
                Currency = "usd",
                ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Service Fee" }
            },
            Quantity = 1
        });
       options.CustomerEmail = customerEmailAddress;
       
        var requestOptions = new RequestOptions
        {
            StripeAccount = stripeAccountID,
            
        };
        var service = new Stripe.Checkout.SessionService();
        
        Stripe.Checkout.Session session = await service.CreateAsync(options, requestOptions);
        _logger.LogInformation($"Stripe session created with ID: {session.Id}");
        _logger.LogInformation($"session details {session.ReturnUrl}", session.Url);
        ///return the client secret to the frontend to complete the payment
        return new Tuple<string, string>(session.ClientSecret, session.Id);
    }

    public static StripeWebHookData GetWebhookEventAndRefIdReceived(string json, IDictionary<string, StringValues> request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null.");
        }

        if (!request.ContainsKey("Stripe-Signature"))
        {
            throw new ArgumentException("Stripe-Signature header is missing in the request.");
        }

        var signature = request["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature))
        {
            throw new ArgumentException("Stripe-Signature header cannot be null or empty.");
        }

        var stripeEvent = EventUtility.ConstructEvent(
               json,
              signature,
               WebhookSecret
           );

        if (stripeEvent == null)
        {
            throw new InvalidOperationException("Failed to construct Stripe event from the request.");
        }
        else if (stripeEvent.Data.Object is Session session && session!=null)
        {
            decimal platformFeeAmt=0, totalFeesForTrans=0;
           
            if (session.Metadata.TryGetValue("PlatformFees", out string? tempId))
            {
                decimal.TryParse(tempId, out platformFeeAmt);
            }
            if (session.Metadata.TryGetValue("TotalFeesForTransaction", out tempId))
            {
                decimal.TryParse(tempId, out totalFeesForTrans);
            }
            return new StripeWebHookData
            {
                EventType = stripeEvent.Type,
                SalesOrderId = int.TryParse(session.ClientReferenceId, out int salesOrderId) ? salesOrderId : 0,
                SessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId,
                PaymentSucceeded = session.PaymentStatus == "paid" ?true:false,
                PlatformFees = platformFeeAmt,
                TotalFeesForTransaction = totalFeesForTrans,
                OrderTotal = session.AmountTotal.HasValue ? session.AmountTotal.Value:0,
                CustomerEmail = session.CustomerEmail
            };
        }
        else if (stripeEvent.Data.Object is Refund refund && refund!=null)
        {
            int orderId=0;
           
            if (refund.Metadata.TryGetValue("SalesOrderId", out string? tempId))
            {
                int.TryParse(tempId, out orderId);
            }
            return new StripeWebHookData
            {
                EventType = stripeEvent.Type,
                SalesOrderId = orderId,
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                RefundStatus = refund.Status,
                RefundAmount = refund.Amount,
                
            };
        }
        else if (stripeEvent.Data.Object is Account account && account!=null)
        {
            account.Metadata.TryGetValue("CustomerId", out string tempId);
            int.TryParse(tempId, out int customerId);
            
            return new StripeWebHookData
            {
                EventType = stripeEvent.Type,
                AccountId =  account.Id,
                CustomerId = customerId,
                DetailsSubmitted = account.DetailsSubmitted,
                RequirementsPending = account.Requirements?.CurrentlyDue.Count > 0
            };
        }
        else
        {
            return new StripeWebHookData();
        }
        //  session.ClientReferenceId
        // return new Tuple<string, string>(stripeEvent.Type, (stripeEvent.Data.Object as Session)?.ClientReferenceId ?? string.Empty);
    }


}

public class StripeWebHookData 
{
    public string EventType { get; set; } = string.Empty;
    public int SalesOrderId { get; set; } = 0;
    public string SessionId { get; set; } = string.Empty;

    public string PaymentIntentId {get; set;} = string.Empty;
    public int CustomerId { get; set; } =0;

    public bool PaymentSucceeded { get; set; } = false;
   public string AccountId { get; set; }= string.Empty;

    public bool DetailsSubmitted { get; set; } = false;

    public bool RequirementsPending { get; set; } = false;

    public string RefundId {get;set; } = string.Empty;

    public long RefundAmount { get; set;} = 0;

    public string RefundStatus { get; set; } = string.Empty;
    public decimal PlatformFees { get; internal set; }
    public decimal OrderTotal { get; internal set; }
    public string CustomerEmail { get; internal set; } = string.Empty;
    public decimal TotalFeesForTransaction { get; internal set; }

    public override string ToString()
    {
        return @$"EventType:{EventType} SalesOrderId: {SalesOrderId} Sessionid { SessionId} 
                CustomerId { CustomerId}  AccountId {AccountId} 
                DetailsSubmitted { DetailsSubmitted == true} RequirementsPending {RequirementsPending}";
    }
}

public class StripeWebHookAccount
{
    public string EventType { get; set; }

    public int CustomerId { get; set; }
}