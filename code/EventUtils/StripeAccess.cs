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
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly string _connectReturnUrl = "https://yourapp.com/stripe/connect";
    private readonly string _connectRefreshUrl = "https://yourapp.com/stripe/refresh";
    private readonly string _paymentReturnUrl = "https://yourapp.com/stripe/payment/success";
    private readonly string _cancelUrl = "https://yourapp.com/stripe/payment/cancel";

    private readonly decimal _applicationFeePercentage = 0.03m; // Example: 10% application fee

    private readonly decimal _fixedTransactionFee = 1.0m; // Example: $0.30 fixed fee per transaction
   
    private static string WebhookSecret { get; set; }

    public StripeAccess(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger)
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
        if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
        {
            throw new ArgumentException("Stripe API key is not configured.");
        }

        StripeConfiguration.ApiKey = configuration["Stripe:ApiKey"];
        WebhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(WebhookSecret))
        {
            throw new ArgumentException("Stripe webhook secret is not configured.");
        }
        _applicationFeePercentage = Convert.ToDecimal(configuration["Stripe:ApplicationFeePercentage"]);
        logger.LogInformation("Initializing Stripe API with provided configuration.");
    }

    public async Task<string> CreateStripeAccount(int customerId)
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

    public async Task<string> InitiateAccountLink(string accountId)
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
                RefreshUrl = _connectRefreshUrl,
                //the url to redirect the client to after they complete the account onboarding process
                ReturnUrl = _connectReturnUrl,
                Type = "account_onboarding",

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
        return (long)(totalAmount * _applicationFeePercentage + lineItems.Count + _fixedTransactionFee); // Example: 10% application fee
    }

    public async Task<Tuple<string,string>> BuySalesItem(int salesOrderId, string stripeAccountID, List<PaymentLineItemModel> lineItems)
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
            Mode = "payment",
            UiMode = "embdedded"

        };
        // options.Metadata = new Dictionary<string, string>
        // {
        //     { "SalesOrderId", salesOrderId.ToString() }
        // };
        options.ClientReferenceId = salesOrderId.ToString();
        options.LineItems.Clear();
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
        ///return the client secret to the frontend to complete the payment
        return new Tuple<string, string>(session.ClientSecret, session.Id);
    }

    public static Tuple<string,string> GetWebhookEventAndRefIdReceived(string json, IDictionary<string,StringValues> request)
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

        //  var session = stripeEvent.Data.Object as Session;
        //  session.ClientReferenceId
        return new Tuple<string, string>(stripeEvent.Type, (stripeEvent.Data.Object as Session)?.ClientReferenceId ?? string.Empty);
    }
    
     
}