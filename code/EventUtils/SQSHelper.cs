
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Net;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly string _emailQueueUrl =string.Empty;

    private readonly string _emailStatusQueueUrl = string.Empty;
    private readonly string _fromEmail = string.Empty;

    private static Microsoft.Extensions.Logging.ILogger? _logger { get; set; }
    public SQSHelper(IConfiguration configuration, ILogger<SQSHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _amazonSQSClient = new AmazonSQSClient(configuration["AccessKeyId"], configuration["AccessKeySecret"],Amazon.RegionEndpoint.USWest2);     
        
        _emailStatusQueueUrl = configuration["SQS:StatusUrl"] ?? throw new Exception("Missing SQS Status QueueUrl.");
        _fromEmail = configuration["FromEmail"] ?? throw new Exception("Missing FromEmail in configuration.");
        _emailQueueUrl = configuration["SQS:QueueUrl"] ?? throw new Exception("Missing SQS QueueUrl.");
    }
    
    public async Task<bool> QueueEmailMessage(string from, string to, string subject, string content, string name, int refID, string messageType="Transactional")
    {
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

        SendMessageResponse response = await _amazonSQSClient.SendMessageAsync(new SendMessageRequest() { QueueUrl = _emailQueueUrl, MessageBody = JsonSerializer.Serialize(tempObj) });
        Console.WriteLine($"Response from queueing message is:{response.HttpStatusCode}");
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
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
    public async Task<bool> QueueMessage(string to, string name, string content, string subject, int refId)
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