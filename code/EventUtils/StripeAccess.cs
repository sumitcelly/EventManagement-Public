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
using Stripe.Tax;
namespace EventUtils;

public class StripeAccess
{
    private readonly Microsoft.Extensions.Logging.ILogger<StripeAccess> _logger;
    private readonly string _connectReturnUrl = "/organizermanager/customerId/stripe";
    private readonly string _connectRefreshUrl = "/organizermanager/customerId/stripe";
    private readonly string _paymentReturnUrl = "/orderpayment/event/event_id?salesOrderId=order_Id&session_id={CHECKOUT_SESSION_ID}";

    private readonly decimal _applicationFeePercentage = 0;// Example: 10% application fee
    private readonly decimal _stripeFeePercentage = 0; // Example: 2.9% Stripe fee

    private readonly long _stripeFixedFee = 0; // Example: 30 cents fixed fee
    private readonly long _floorFee = 0; // Example: 35 cents minimum application fee

    private static string WebhookSecret { get; set; }
    private static  LocationService locationService = new LocationService();

     private static  TransferService transferService = new TransferService();

    private static string _platformAcctId = "";

    private static string _baseUrl="";

    public static readonly Dictionary<string, string> EventCategoryTaxMapping = new Dictionary<string, string>
    {
        { "General Event", "txcd_50010001" },
        { "Nightclub or Bar Event", "txcd_50013002" },
        { "Museum or Art Gallery", "txcd_50011003" },
        { "Conference or Workshop", "txcd_50013001" },
        { "Sporting Event", "txcd_50012001" },
        {"Concert or Live Performance", "txcd_50010003" }
        // Add more mappings as needed
    };

    public static readonly string _defaultTicketTaxCode = "txcd_50010001"; // Default tax code if category is not found
    
    public static readonly string _serviceTaxCode = "txcd_20030000"; // Tax code for service fees

    public static readonly string _defaultMechandiseTaxCode = "txcd_99999999"; // Default tax code for merchandise

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
        _platformAcctId = configuration["Stripe:PlatformId"];
        _baseUrl = configuration["BaseFrontEndUrl"]?? "http://localhost:5173";
        if (string.IsNullOrEmpty(WebhookSecret))
        {
            throw new ArgumentException("Stripe webhook secret is not configured.");
        }
       
        _applicationFeePercentage= Convert.ToDecimal(configuration["Fees:Platform"]);
        _stripeFeePercentage= Convert.ToDecimal(configuration["Fees:Stripe"]);
        _stripeFixedFee= Convert.ToInt64(configuration["Fees:StripeFixed"]);
        _floorFee= Convert.ToInt64(configuration["Fees:Floor"]);

        logger.LogInformation($"Initializing Stripe API with provided configuration. {StripeConfiguration.ApiVersion}");
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

    public async Task<string> GetConnectedAccountTaxStatus(string connectedAcctId)
    {
        if (string.IsNullOrWhiteSpace(connectedAcctId))
        {
            throw new ArgumentNullException(nameof(connectedAcctId));
        }

     
        var options = new Stripe.Tax.SettingsGetOptions();
        var requestOptions = new RequestOptions
        {
            StripeAccount = connectedAcctId,
        };
        SettingsService svc= new SettingsService();
        try
        {
            Settings taxSettings= await  svc.GetAsync(options, requestOptions);
            return taxSettings.Status;
        }
        catch(Exception ex)
        {
            _logger.LogError("Error retrieving tax settings for account {0} with error {1}",connectedAcctId, ex.Message);
            return string.Empty;
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
                RefreshUrl = _baseUrl +_connectRefreshUrl.Replace("customerId",organizerId.ToString()),
                //the url to redirect the client to after they complete the account onboarding process
                ReturnUrl = _baseUrl+ _connectReturnUrl.Replace("customerId",organizerId.ToString()),
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

    private long CalculatePlatformFee(List<PaymentLineItemModel> lineItems, decimal appFeePercentage)
    {
        decimal totalAmount = 0;
        foreach (var item in lineItems)
        {
            totalAmount += item.Price * item.Quantity;
        }
        long i = Convert.ToInt64(totalAmount * appFeePercentage *100); // Example: 10% application fee
        _logger.LogInformation($"application fees amount is {i} using fees percent {appFeePercentage}.");
        return Math.Max(i, _floorFee); // Ensure the fee is at least the floor fee
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

    /*
    using Stripe;
using System;
using System.Threading.Tasks;

public class StripeRefundHandler
{
    public async Task ProcessFullRefund(string paymentIntentId, long totalToCustomerCents, long platformFeeCents)
    {
        // 1. Retrieve the PaymentIntent with expanded latest_charge
        var piService = new PaymentIntentService();
        var piOptions = new PaymentIntentGetOptions();
        piOptions.AddExpand("latest_charge");

        var paymentIntent = await piService.GetAsync(paymentIntentId, piOptions);

        // 2. Safety Check: Only refund successful payments
        if (paymentIntent.Status != "succeeded")
        {
            throw new Exception($"Cannot refund PaymentIntent in status: {paymentIntent.Status}");
        }

        var charge = paymentIntent.LatestCharge;
        string feeId = charge.ApplicationFeeId;

        // 3. Step One: Refund the customer for the Ticket portion
        // We set RefundApplicationFee to FALSE to control the split manually
        var refundService = new RefundService();
        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = totalToCustomerCents, 
            RefundApplicationFee = false, // Prevents Stripe from guessing proportions
            Reason = "requested_by_customer"
        };
        
        Refund refund = await refundService.CreateAsync(refundOptions);

        // 4. Step Two: Refund the exact Tax + Profit from your platform account
        // This pulls the money you previously reclaimed/withheld back to the customer
        if (!string.IsNullOrEmpty(feeId))
        {
            var feeRefundService = new ApplicationFeeRefundService();
            var feeOptions = new ApplicationFeeRefundCreateOptions
            {
                Amount = platformFeeCents // e.g., 1300 for $10 Tax + $3 Profit
            };

            await feeRefundService.CreateAsync(feeId, feeOptions);
        }
    }
}
*/
    /// <summary>
    /// Refunds the amount in cents for sales order id. The amount is expected to be the total price of the ticket(s), 
    /// no fees included since fees are not refunded.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="salesOrderId"></param>
    /// <param name="paymentIntentId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<(bool refundStatus, string refundId, bool isPartial)> RefundSalesOrder(decimal amount, decimal tax, int salesOrderId,
                        string paymentIntentId, string stripeAcctId, string customerEmail)
    {
        if (amount <=0 || string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogError($"Cannot process refund if amount {amount} is 0 or payment intent id {paymentIntentId} is empty ");
            throw new ArgumentException("Either amount or payment intentId is invalid");
        }
        if (string.IsNullOrWhiteSpace(stripeAcctId))
        {
            throw new ArgumentNullException(stripeAcctId, nameof(stripeAcctId));
        }
        try
        {
                // 1. Retrieve the PaymentIntent with expanded latest_charge
            var piService = new PaymentIntentService();
            var piOptions = new PaymentIntentGetOptions()
            {
                 
            };
            piOptions.AddExpand("latest_charge");
            var requestOptions = new RequestOptions { StripeAccount = stripeAcctId };

            var paymentIntent = await piService.GetAsync(paymentIntentId, piOptions,requestOptions);

            // 2. Safety Check: Only refund successful payments
            if (paymentIntent.Status != "succeeded")
            {
                throw new Exception($"Cannot refund PaymentIntent in status: {paymentIntent.Status}");
            }

            RefundService _refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                //TODO revisit when we start collecting tax
                //if tax was added to the app fee when creating the transsaction,
                //it will get refunded as well. Right now we are not adding tax.
                //this will pull the application fee amount from my platform account and give it to
                //customer. But the amount requested must include the app fee amount. So totalticketcost+platformfees
                RefundApplicationFee = false,      
                //set the amoun to whatever should go back to the customer (ticket + appfees +tax )
                ///must set RefundApplicationFee so that fees+tax is pulled form the platform not connected account
                Amount = (long)amount,
                // Amount is optional here; Stripe refunds the full remaining amount by default
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    { "SalesOrderId", salesOrderId.ToString() },
                    {"CustomerEmail",customerEmail}
                   
                }
            };  

            Refund refund =  await _refundService.CreateAsync(options,requestOptions);
           
            // if (tax >0)
            // {
            //    var transferOptions = new TransferCreateOptions
            //     {
            //         Amount = (long)tax, // The $5.00 tax you pulled earlier
            //         Currency = "usd",
            //         Destination = stripeAcctId,
            //         Description = "Returning tax funds for customer refund",
            //        // SourceTransaction = paymentIntent.LatestCharge.Id
                   
            //     };
            //     var transferService = new TransferService();
            //     await transferService.CreateAsync(transferOptions);
                

            // }
            _logger.LogInformation($"Status of refund for order id {salesOrderId} is {refund.Status}");
            _logger.LogInformation($"Refund object for sales order id {salesOrderId} is {refund.ToJson()}");
       
            return (refund.Status == "succeeded", refund.Id, refund.Amount < amount);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Exception when processing refund {ex}");
            throw;
        }

    }
    
    public async Task<string> GetLocationIdForAddress(string eventName,string streetAddress, string city, string state, string zipCode, string organizerStripeId, string country="US")
    {
        
        // 1. Initialize the service
       

        // 2. Define the venue address and type
        var options = new LocationCreateOptions
        {
            Address = new AddressOptions
            {
                Line1 = streetAddress, 
                City = city,
                State = state,
                PostalCode = zipCode,
                Country = country,
            },
            // For tickets and events, the type MUST be 'performance'
            Type = "performance", 
            Description = eventName,

        };

        // 3. Create the location in Stripe
        Location location = await locationService.CreateAsync(options,new RequestOptions { 
                StripeAccount = organizerStripeId
        });
        return location.Id;
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
    public async Task<Tuple<string, string>> CreateCheckoutSession(string stripeAccountID, PaymentModel paymentModel,
                                                                string customerEmailAddress, string zipCode,
                                                                bool passOnAllFeesToCustomer = false,
                                                                decimal? platformFeeOverride = null)
    {
        
        if (string.IsNullOrEmpty(stripeAccountID))
        {
            throw new ArgumentException("Stripe customer ID cannot be null or empty.", nameof(stripeAccountID));
        }
        if (paymentModel == null || paymentModel.LineItems == null || paymentModel.LineItems.Count == 0)
        {
            throw new ArgumentException("payment model or line items cannot be null or empty.", nameof(paymentModel));
        }

        if (string.IsNullOrWhiteSpace(paymentModel.LocationId))
        {
            _logger.LogError($"Event location id is missing for event id {paymentModel.EventId}");
            throw new ArgumentException("Event location id is missing. Please provide complete address information for the event.");
        }
        else
        {
            _logger.LogInformation($"location id is {paymentModel.LocationId}");
        }

        if (string.IsNullOrWhiteSpace(paymentModel.EventCategory))
        {
            _logger.LogWarning($"Event category is not provided for event id {paymentModel.EventId}. Default tax code {_defaultTicketTaxCode} will be applied.");        
        }

        string ticketTaxCode = EventCategoryTaxMapping.ContainsKey(paymentModel.EventCategory) ? EventCategoryTaxMapping[paymentModel.EventCategory] : _defaultTicketTaxCode;
        _logger.LogInformation($"Tax code {ticketTaxCode} will be applied for event category {paymentModel.EventCategory} and event id {paymentModel.EventId}.");
        
        long appFees = CalculatePlatformFee(paymentModel.LineItems, platformFeeOverride ?? _applicationFeePercentage);
        long totalItemsUnitPrice = (long)paymentModel.LineItems.Sum(item => item.Price * item.Quantity * 100);
        _logger.LogInformation($"Total items price in cents: {totalItemsUnitPrice}");
        
        //From this article, https://docs.stripe.com/tax/tax-for-platforms
        //it seems I cannot even file for taxes with the direct charge model that I have.
        //So no point calculating. We still need this so that I can accurately calculate the tax 
        //since that changes the stripe fees and the customer gets a bit less (.029* tax)
        decimal amountaftertax= await CalculateAmountAfterTax(zipCode,totalItemsUnitPrice,paymentModel.LocationId,
                                    ticketTaxCode,appFees, stripeAccountID);

        decimal tax= amountaftertax-appFees-totalItemsUnitPrice;
        _logger.LogInformation($"Amount after tax {amountaftertax} and tax is {tax}");  
        long finalTotal=0, totalFeesForTrans=0, stripeFee=0;
        if (passOnAllFeesToCustomer)
        {
           (finalTotal, totalFeesForTrans, stripeFee) = StripeFeeCalculator.Calculate(totalItemsUnitPrice, appFees,(long)tax, (double)_stripeFeePercentage, _stripeFixedFee);
        }
        //absorb the stripe fees but pass on the application fees
        else
        {
            //stripeFee = (long)(30 +(.029)*totalItemsUnitPrice);
            finalTotal = totalItemsUnitPrice + appFees;
            totalFeesForTrans = appFees;
        }
        
        _logger.LogInformation($"Fees after stripe calculation is {finalTotal} {totalFeesForTrans}");

        _logger.LogInformation($"Processing purchase for customer: {stripeAccountID} with {paymentModel.LineItems.Count} line items.");

        var options = new SessionCreateOptions
        {
            ReturnUrl = _baseUrl+_paymentReturnUrl.Replace("event_id", paymentModel.EventId.ToString()).Replace("order_Id", paymentModel.SalesOrderId.ToString()),
            PaymentMethodTypes = new List<string>
            {
              "card"
            
            },
            
            Metadata = new Dictionary<string, string>
            {
                { "SalesOrderId", paymentModel.SalesOrderId.ToString() },
                { "EventId", paymentModel.EventId.ToString() },
                { "PlatformFees", appFees.ToString() },
                { "TotalFeesForTransaction", totalFeesForTrans.ToString() },
                { "PassOnAllFeesToCustomer", passOnAllFeesToCustomer.ToString() }
            },
            PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
            {
                //add the tax to appFees if you want to move tax to our account and 
                //pay for organizer.
                ApplicationFeeAmount = appFees
            },
            
            //one time Fpayment
            Mode = "payment",
            UiMode = "embedded_page"

        };
        
        options.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };
        options.BillingAddressCollection = "required";
        options.ClientReferenceId = paymentModel.SalesOrderId.ToString();
     
        options.LineItems = new List<SessionLineItemOptions>();
      
        foreach (var item in paymentModel.LineItems)
        {   
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
            
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                         
                        Name = item.Description,
                        TaxDetails = new SessionLineItemPriceDataProductDataTaxDetailsOptions()
                        {
                            PerformanceLocation = paymentModel.LocationId,
                             //use mechandise tax code for add on items since they are not event admission tickets, this is to handle the case where an order has both tickets and add on items
                            TaxCode = item.IsAddOn ? _defaultMechandiseTaxCode: ticketTaxCode
                        },
                        
                    },
                    UnitAmount = (long)(item.Price * 100), // Convert to cents
                },
                Quantity = item.Quantity,
             });
        }
      
     
        options.LineItems.Add(new SessionLineItemOptions {
            PriceData = new SessionLineItemPriceDataOptions {
                UnitAmount = appFees, // The remaining "Service Fee" (approx $1.95)
                Currency = "usd",
                 
                ProductData = new SessionLineItemPriceDataProductDataOptions { 
                    Name = "Platform Fees" ,      
                      TaxDetails = new SessionLineItemPriceDataProductDataTaxDetailsOptions()
                        {
                             
                            PerformanceLocation = paymentModel.LocationId,
                             //use mechandise tax code for add on items since they are not event admission tickets, this is to handle the case where an order has both tickets and add on items
                            TaxCode = ticketTaxCode
                        },
                        } // Tax code for service fees
            },
            Quantity = 1
            }
        );
        if (stripeFee >0)
        {
            options.LineItems.Add(new SessionLineItemOptions()
            {
                PriceData = new SessionLineItemPriceDataOptions {
                UnitAmount = stripeFee, // The remaining "Service Fee" (approx $1.95)
                Currency = "usd",
                 
                ProductData = new SessionLineItemPriceDataProductDataOptions { 
                    Name = "Stripe Fees" ,
                    TaxDetails = new SessionLineItemPriceDataProductDataTaxDetailsOptions()
                        {
                             
                            PerformanceLocation = paymentModel.LocationId,
                             //use mechandise tax code for add on items since they are not event admission tickets, this is to handle the case where an order has both tickets and add on items
                            TaxCode = _serviceTaxCode
                        },}
                },
                Quantity = 1
            }
            );
        }
        
        options.CustomerEmail = customerEmailAddress;
        
       //this is a direct charge model where the connected acct is the merchant of record.
       //Not a destination charge model. The payment intent is on the organizer.
       //We are not transferring money to the connected account. Just take a cut of the fees.
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


    public async Task<decimal> CalculateAmountAfterTax(string zipCode,decimal ticketCost, string perfLocation,
                string taxCode, decimal platformFees, string stripeAccount,string country="US")
    {
        _logger.LogInformation($"Ticket cose {ticketCost},perfLocation {perfLocation}, fees {platformFees}, tax code {taxCode} ");
        var taxService = new Stripe.Tax.CalculationService();

        // 2. Prepare the calculation request
        var calcOptions = new Stripe.Tax.CalculationCreateOptions
        {
            Currency = "usd",
            CustomerDetails = new Stripe.Tax.CalculationCustomerDetailsOptions
            {
                // Address is required to determine the buyer's location
                Address = new AddressOptions { PostalCode = zipCode, Country = country },
                AddressSource = "billing" 
            },
            LineItems = new List<Stripe.Tax.CalculationLineItemOptions>
            {
                new Stripe.Tax.CalculationLineItemOptions
                {
                    Amount = (long)ticketCost, // $20.00 Ticket Price in cents
                    Reference = "ticket_line_item", // Unique ID for this calculation
                    TaxCode = taxCode, // E.g., Concert / Live Performance
                    // The "Magic" location ID for Denver/Boulder
                    PerformanceLocation = perfLocation ,
                    
                },
                new Stripe.Tax.CalculationLineItemOptions
                {
                    Amount = (long)platformFees, // $20.00 Ticket Price in cents
                    Reference = "platform_fees", // Unique ID for this calculation
                    TaxCode = taxCode, // E.g., Concert / Live Performance
                    // The "Magic" location ID for Denver/Boulder
                    PerformanceLocation = perfLocation 
                }
            }
        };

        try
        {
        // 3. Execute the calculation
            var calculation = await taxService.CreateAsync(calcOptions, new RequestOptions()
            {
                 StripeAccount = stripeAccount
            });
            return calculation.AmountTotal;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calculating tax: {ex.Message}");
            return 0;
        }

    }
    public async Task<bool>  CollectTax(string fromStripeAcctId,string paymentIntentId, decimal amount,
                                        int orderId,string description)
    {

        try 
        {
            
            var piService = new PaymentIntentService();

            var getOptions = new PaymentIntentGetOptions
            {
                // Optionally expand 'latest_charge' if your SDK version supports it 
                // to ensure the field is fully populated.
                Expand = new List<string> { "latest_charge", "latest_charge.transfer"}
            };
            var pi = await piService.GetAsync("pi_3TQxpG1gRFvi6FYp1g0XBBLi", getOptions, new RequestOptions 
            { 
                StripeAccount = fromStripeAcctId,
            });

             var transferId = string.Empty;
            if (pi.LatestCharge?.Transfer?.Id == null || pi.LatestCharge.TransferId == null)
            {
                // Transfer hasn't been created yet - try fetching it from Transfers list
                var transferService = new TransferService();
               
               TransferListOptions options1 = new TransferListOptions()
               {
                Destination= fromStripeAcctId,
                   Limit=100
               };
               //options1.AddExtraParam("DestinationPayment",pi.LatestChargeId);
                var transfers = await transferService.ListAsync(options1, new RequestOptions { StripeAccount = fromStripeAcctId });
                
                var transfer = transfers.Data.FirstOrDefault();
                if (transfer != null)
                    transferId = transfer.Id;
                else
                    throw new Exception($"No transfer found for charge {pi.LatestChargeId}");
            }
            else
            {
                transferId = pi.LatestCharge.Transfer.Id ?? pi.LatestCharge.TransferId;
            }

// 2. Find the transfer that was created from this specific charge
            
        // Using 'm' makes 0.029 a decimal, allowing it to work with your amount
           long amountToPull = (long)Math.Round(amount - (amount * 0.029m));


            // 3. Now pull the tax back from that specific transfer
            var reversalService = new TransferReversalService();
            var reversalOptions = new TransferReversalCreateOptions
            {
                Amount = amountToPull, // This is the exact CO tax calculated
                Description = "Reclaiming tax for Marketplace Facilitator remittance",
                
            };
            var reversal =  await reversalService.CreateAsync(transferId, reversalOptions);

            // // 2. Extract the Charge ID (ch_...)
            // string chargeId = pi.LatestChargeId; 
            // var options = new  TransferCreateOptions
            // {
            //     Amount = (long)amount,
            //     Currency = "usd",
            //     Destination = _platformAcctId, // Your acct_xxx
            //     Description = description,
            //     Metadata = new Dictionary<string, string>
            //     {
            //         { "original_payment_intent", paymentIntentId },
            //         { "order_id",orderId.ToString()},
            //         { "type", "sales_tax_recovery" }
            //     }
            // };

            // // Act as the organizer to push the tax to your platform
            // var requestOptions = new RequestOptions { StripeAccount = fromStripeAcctId };
            
            // Transfer transfer = await transferService.CreateAsync(options, requestOptions);
            _logger.LogInformation($"Collect tax result for {paymentIntentId} is {reversal.ToJson()}");
            return true;
        }
        catch (StripeException ex) when (ex.Message.Contains("insufficient funds"))
        {
            // Money is likely still 'Pending' in the organizer's account.
            // Leave as 'Pending' to try again tomorrow.
            _logger.LogInformation($"Funds not yet available for Order {orderId} for request {description}");
            return false;
        }
        catch(Exception exc)
        {
            _logger.LogError($"Error in transferring tax for {description} and order id {orderId}: {exc.Message}");
            return false;
        }
      
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
               WebhookSecret,
               throwOnApiVersionMismatch: false
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
                CustomerEmail = session.CustomerEmail,
                TotalTax = session.TotalDetails?.AmountTax ?? 0,
            };
        }
        else if (stripeEvent.Data.Object is Refund refund && refund!=null)
        {
            int orderId=0;
            
            if (refund.Metadata.TryGetValue("SalesOrderId", out string? tempId))
            {
                int.TryParse(tempId, out orderId);
            }
            refund.Metadata.TryGetValue("CustomerEmail", out string? email);
            
            return new StripeWebHookData
            {
                EventType = stripeEvent.Type,
                SalesOrderId = orderId,
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                RefundStatus = refund.Status,
                RefundAmount = refund.Amount,
                CustomerEmail = email ?? string.Empty,
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
                ChargedEnabled = account.ChargesEnabled,
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
    public long TotalTax { get; internal set; }

    public bool ChargedEnabled {get;set;} = false;

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

