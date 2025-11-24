
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly string _queueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/NotificationEventPr0";

    private readonly string _fromEmail = "support@polkadotsandcurry.com";

    public SQSHelper(IConfiguration configuration)
    {
        
        _amazonSQSClient = new AmazonSQSClient(configuration["AccessKeyId"], configuration["AccessKeySecret"],Amazon.RegionEndpoint.USWest2);     
        
    }
    
    public async Task<bool> QueueEmailMessage(string from, string to, string subject, string content, string name)
    {
        Email tempObj = new Email()
        {
            From = from,
            To = to,
            Subject = subject,
            Body = content,
            Name = name
        };


        SendMessageResponse response = await _amazonSQSClient.SendMessageAsync(new SendMessageRequest() { QueueUrl = _queueUrl, MessageBody = JsonSerializer.Serialize(tempObj) });
        Console.WriteLine($"Response from queueing message is:{response.HttpStatusCode}");
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    } 

    public async Task<bool> QueueMessage(string to, string name, string content, string subject)
    {
        var messageAttributes = new Email()
        {
            From = _fromEmail,
            To = to,
            Subject = subject,
            Body = content,
            Name = name
        };
       
        SendMessageResponse response = await _amazonSQSClient.SendMessageAsync(new SendMessageRequest() 
            { QueueUrl = _queueUrl, MessageBody = JsonSerializer.Serialize(messageAttributes) });
        Console.WriteLine($"Response from queueing message is:{response.HttpStatusCode}");
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
      
    }
}

public class Email
{
    public required string From { get; set; }

    public required string To { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }
    
    public required string Name { get; set; }
}