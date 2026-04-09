using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using ScratchLambda;
using System.ComponentModel;
using System.Net;
using System.Text.Json;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly ILambdaLogger _logger;
    private readonly string _emailStatusQueueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/EmailStatus";


    public SQSHelper(ILambdaLogger logger)
    {
         _amazonSQSClient = new AmazonSQSClient(Amazon.RegionEndpoint.USWest2);
        _logger = logger;
            
    }
    
 
    public async Task<bool> QueueEmailStatusMessage(EmailStatusUpdate message)
    {
        SendMessageResponse response = await _amazonSQSClient.SendMessageAsync(new SendMessageRequest() { QueueUrl = _emailStatusQueueUrl, MessageBody = JsonSerializer.Serialize(message) });
        Console.WriteLine($"Response from queueing message is:{response.HttpStatusCode}");
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    } 

   
}

public class EmailStatusUpdate
{
   
    public int Id { get; set;} //can be id of emailrecipients table or email tran log table depending on message type

    public required string MessageType { get; set; } //campaign or transactional

    public required string RecipientEmail { get; set; } 

    public required string Status { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string SenderMessageId { get; set; } = string.Empty;

    public DateTime? SentAt { get; set; }
}



