using System.Runtime.InteropServices;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal.Util;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace ScratchLambda;

public class NotificationSender
{
    private static  AmazonSimpleEmailServiceClient _emailClient = null;
    private static ILambdaLogger _logger = null;
    public static void Init(ILambdaLogger logger)
    {
        _emailClient = new AmazonSimpleEmailServiceClient(new
                            AmazonSimpleEmailServiceConfig()
        { Profile = new Amazon.Profile("SC") });
        _logger = logger;
    }

    //public static ILambdaLogger Logger { get; set; }
    
    /// <summary>
    /// REturns message ID if successful and / or stopsending if a serious error occured
    /// </summary>
    /// <param name="toAddress"></param>
    /// <param name="subject"></param>
    /// <param name="bodyText"></param>
    /// <param name="fromAddress"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<Tuple<string, bool>> SendEmail(string toAddress, string subject, string bodyText, string fromAddress)
    {
        if (string.IsNullOrWhiteSpace(toAddress) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(fromAddress) ||
            string.IsNullOrWhiteSpace(bodyText) ||
            string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new ArgumentNullException("Invalid args sent for Send Email. One of them must be null.");
        }

 
        //"<html><body><h2>Hello from Amazon SES</h2><ul><li>I'm a list item</li><li>So am I!</li></body></html>")
        var sendRequest = new SendEmailRequest
        {
            Source = fromAddress,
            Destination = new Destination { ToAddresses = new List<string>() { toAddress } },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content { Charset = "UTF-8", Data = bodyText },
                    Text = new Content { Charset = "UTF-8", Data = "Text content for message" }
                }
            },
            ConfigurationSetName = "EventsProSender"
        };

        bool stopSending = false;
        string responseId = string.Empty;
        try
        {
            var sendResponse = await _emailClient.SendEmailAsync(sendRequest);
            responseId = sendResponse.MessageId;
        }
        catch (AmazonSimpleEmailServiceException ex)
        {
            string errorMessage = $"Amazon SES exception: {ex.Message}";
            _logger.LogError(errorMessage);
        }
        catch (Amazon.Runtime.AmazonServiceException ex)
        {
            // Network connectivity problems (ex: name resolution failure) can cause this exception and ErrorCode is null.
            if (ex.ErrorCode != null && ex.ErrorCode.Equals("Throttling"))
            {
                _logger.Log("Amazon SES throttling error: " + ex.Message);
            }
            else
            {
                _logger.Log($"Amazon service failure: {ex.Message}");
            }
            stopSending = true;
        }
        catch (Exception ex)
        {
            _logger.Log($"Amazon Sending failure: {ex.Message}");
        }

        return new Tuple<string, bool>(responseId, stopSending);

    }

}