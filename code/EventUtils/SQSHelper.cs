
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
public class SQSHelper
{
    private  readonly AmazonSQSClient _amazonSQSClient;
    private  readonly string _queueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/NotificationEventPr0";


    public SQSHelper(IConfiguration configuration)
    {
        _amazonSQSClient = new AmazonSQSClient(configuration["AccessKeyId"], configuration["AccessKeySecret"]);     
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
}

public class Email
{
    public string From { get; set; }

    public string To { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }
    
    public string Name { get; set; }
}