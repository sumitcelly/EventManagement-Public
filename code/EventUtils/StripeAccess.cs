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
    private readonly string _paymentReturnUrl = "https://yourapp.com/stripe/payment/success";
    private readonly string _cancelUrl = "https://yourapp.com/stripe/payment/cancel";

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
        _applicationFeePercentage = Convert.ToDecimal(configuration["Stripe:ApplicationFeePercentage"]);
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

    private long CalculateApplicationFee(List<PaymentLineItemModel> lineItems)
    {
        decimal totalAmount = 0;
        foreach (var item in lineItems)
        {
            totalAmount += item.Price * item.Quantity;
        }
        long i = Convert.ToInt64(totalAmount * _applicationFeePercentage *100); // Example: 10% application fee
        _logger.LogInformation($"fees amount is {i}");
        return i;
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
    public async Task<Tuple<string, string>> BuySalesItem(int salesOrderId, string stripeAccountID, List<PaymentLineItemModel> lineItems)
    {
        if (string.IsNullOrEmpty(stripeAccountID))
        {
            throw new ArgumentException("Stripe customer ID cannot be null or empty.", nameof(stripeAccountID));
        }
        if (lineItems == null || lineItems.Count == 0)
        {
            throw new ArgumentException("Line items cannot be null or empty.", nameof(lineItems));
        }

        _logger.LogInformation($"Processing purchase for customer: {stripeAccountID} with {lineItems.Count} line items.");
        var options = new Stripe.Checkout.SessionCreateOptions
        {
            SuccessUrl = _paymentReturnUrl,
            CancelUrl = _cancelUrl,
            PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = CalculateApplicationFee(lineItems),
            },
            //one time payment
            Mode = "payment",
            UiMode = "embedded"

        };
        // options.Metadata = new Dictionary<string, string>
        // {
        //     { "SalesOrderId", salesOrderId.ToString() }
        // };
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
            return new StripeWebHookData
            {
                EventType = stripeEvent.Type,
                SalesOrderId = int.TryParse(session.ClientReferenceId, out int salesOrderId) ? salesOrderId : 0,
                SessionId = session.Id,
                PaymentSucceeded = session.PaymentStatus == "paid" ?true:false
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
    public int CustomerId { get; set; } =0;

    public bool PaymentSucceeded { get; set; } = false;
   public string AccountId { get; set; }= string.Empty;

    public bool DetailsSubmitted { get; set; } = false;

    public bool RequirementsPending { get; set; } = false;
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