
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly string _emailQueueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/NotificationEventPr0";

    private readonly string _emailStatusQueueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/EmailStatus";
    private readonly string _fromEmail = "support@polkadotsandcurry.com";

    private static Microsoft.Extensions.Logging.ILogger? _logger { get; set; }
    public SQSHelper(IConfiguration configuration, ILogger<SQSHelper> logger)
    {
        _logger = logger;
        _amazonSQSClient = new AmazonSQSClient(configuration["AccessKeyId"], configuration["AccessKeySecret"],Amazon.RegionEndpoint.USWest2);     
        
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
        var updates = new List<EmailStatusUpdate>();
        foreach(var message in response.Messages)
        {
            try
            {
                var update = JsonSerializer.Deserialize<EmailStatusUpdate>(message.Body);
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

    public void DeleteMessagesFromEmailStatus(List<string> list)
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
       _amazonSQSClient.DeleteMessageBatchAsync(deleteRequest);
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