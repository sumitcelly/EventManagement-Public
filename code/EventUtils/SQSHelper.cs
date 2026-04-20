
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Net;
using Amazon.Runtime;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly string _emailQueueUrl =string.Empty;

    private readonly string _emailStatusQueueUrl = string.Empty;
    private readonly string _fromEmail = string.Empty;

    private static Microsoft.Extensions.Logging.ILogger _logger { get; set; }
    public SQSHelper(IConfiguration configuration, ILogger<SQSHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _amazonSQSClient = new AmazonSQSClient(configuration["AccessKeyId"], configuration["AccessKeySecret"],Amazon.RegionEndpoint.USWest2);     
        
        _emailStatusQueueUrl = configuration["SQS:StatusUrl"] ?? throw new Exception("Missing SQS Status QueueUrl.");
        _fromEmail = configuration["FromEmail"] ?? throw new Exception("Missing FromEmail in configuration.");
        _emailQueueUrl = configuration["SQS:QueueUrl"] ?? throw new Exception("Missing SQS QueueUrl.");
    }
    
    public async Task<QueueResponse> QueueEmailMessage(string from, string to, string subject, string content, string name, int refID, string messageType="Transactional")
    {
        QueueResponse resp = new QueueResponse { Success = false, Retry = false };
        Email tempObj = new Email()
        {
            From = from,
            To = to,
            Subject = subject,
            Body = content,
            Name = name,
            RefID = refID,
            MessageType = messageType
        };
        try
        {
            _logger.LogInformation($"Queueing email message to SQS. To: {to}, Subject: {subject}, RefID: {refID}, MessageType: {messageType}");
            SendMessageResponse response = await _amazonSQSClient.SendMessageAsync(new SendMessageRequest() { QueueUrl = _emailQueueUrl, MessageBody = JsonSerializer.Serialize(tempObj) });
           _logger.LogInformation($"SQS SendMessage response. HTTP Status: {response.HttpStatusCode}, MessageId: {response.MessageId}");
            resp.Success= response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            resp.Retry = !resp.Success; // Retry if the message failed to queue
        }
        // 1. Specific SQS Service Errors
        catch (InvalidMessageContentsException ex)
        {
            // Permanent failure: The message body has invalid characters. 
            // Logging is critical here to debug the source data.
            resp.Retry = false; // Don't retry, this will fail every time until the underlying data issue is fixed.
            resp.Message = ex.Message;
            _logger.LogError($"Invalid message content: {ex.Message}");
        }
        catch (QueueDoesNotExistException ex)
        {
            resp.Retry= false;
            resp.Message = ex.Message;
            // Critical failure: The Queue URL is likely wrong or the queue was deleted.
           _logger.LogError($"Target queue not found: {ex.Message}");
        }
        // 2. General SQS Failures (includes Throttling)
        catch (AmazonSQSException ex)
        {
            resp.Message = ex.Message;
            resp.Retry =false; // Depending on the error code, you might want to set this to true for transient errors like throttling (e.g., "ThrottlingException")
            // The SDK automatically retries transient errors like throttling up to 4 times by default.
            // If you hit this block, all retries have already failed.
            _logger.LogError($"SQS specific error (Code: {ex.ErrorCode}): {ex.Message}");
        }
        // 3. Client-side or Network Issues
        catch (AmazonClientException ex)
        {
            resp.Message = ex.Message;
            resp.Retry =true;
            // Thrown for local issues (credentials, no internet) or if a Task was cancelled.
            _logger.LogError($"Client-side error: {ex.Message}");
        }
        // 4. Fallback for any other AWS error
        catch (AmazonServiceException ex)
        {
            resp.Message = ex.Message;
            resp.Retry =true;
            _logger.LogError($"General AWS service error: {ex.Message}");
        }
        catch(Exception ex)
        {
            resp.Message = ex.Message;
            //resp.Retry =true;
            _logger.LogError($"Error email queueing: {ex.Message}");
        }

        return resp;
       
    } 

    public async Task<List<EmailStatusUpdate>> GetEmailStatusUpdates()
    {
        var response = await _amazonSQSClient.ReceiveMessageAsync(new ReceiveMessageRequest() { QueueUrl = _emailStatusQueueUrl, MaxNumberOfMessages = 10 });
        _logger?.LogInformation($"Polled email status queue. HTTP Status: {response.HttpStatusCode}, Messages Received: {response.Messages?.Count ?? 0}");
        if (response !=null && response.HttpStatusCode == System.Net.HttpStatusCode.OK && response.Messages != null && response.Messages.Count > 0)
        {
            _logger?.LogInformation($"Received {response?.Messages?.Count} messages from email status queue.");
        }
         else
        {
            _logger?.LogInformation($"No messages received from email status queue.");
            return new List<EmailStatusUpdate>();
        }
        var updates = new List<EmailStatusUpdate>();
        foreach(var message in response.Messages)
        {
            try
            {
                var update = JsonSerializer.Deserialize<EmailStatusUpdate>(message.Body);
                _logger.LogInformation("received email status update: " + message.Body);
                if (update != null)
                {
                    update.ReceiptHandle = message.ReceiptHandle;
                   
                    updates.Add(update);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error processing message: {ex.Message}");
            }
            
        }
        return updates;
    }
    public async Task<QueueResponse> QueueMessage(string to, string name, string content, string subject, int refId)
    {

        return await QueueEmailMessage(_fromEmail, to, subject, content, name, refId);
      
    }

    public async Task<bool> DeleteMessagesFromEmailStatus(List<string> list)
    {
        DeleteMessageBatchRequest deleteRequest = new DeleteMessageBatchRequest
        {
            QueueUrl = _emailStatusQueueUrl ,
            Entries = list.Select((receiptHandle, index) => new DeleteMessageBatchRequestEntry
            {
                Id = index.ToString(),
                ReceiptHandle = receiptHandle
            }).ToList()
        };
       DeleteMessageBatchResponse resp=  await _amazonSQSClient.DeleteMessageBatchAsync(deleteRequest);
       return resp.HttpStatusCode == HttpStatusCode.OK;
    }
}

public class EmailStatusUpdate
{


    public string ReceiptHandle { get; set; }=string.Empty; // This is needed to delete the message from SQS after processing
    public int Id { get; set;} //can be id of emailrecipients table or email tran log table depending on message type

    public required string MessageType { get; set; } //campaign or transactional

    public required string RecipientEmail { get; set; } 

    public required string Status { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string SenderMessageId { get; set; } = string.Empty;

    public DateTime? SentAt { get; set; }
}

public class QueueResponse
{
    public bool Retry { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class Email
{
    public required string From { get; set; }

    public required string To { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }
    
    public required string Name { get; set; }

    public int RefID { get; set; } //can be used to store pk of emailrecipients or emailtransactionlog depending on message type

    public string MessageType { get; set; } = "Transactional"; //Transactional or Campaign
}