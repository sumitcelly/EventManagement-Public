
using Amazon.Runtime.Internal.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
public class SQSHelper
{
    private static readonly AmazonSQSClient _amazonSQSClient;
    private static readonly string _queueUrl = "https://sqs.us-west-2.amazonaws.com/975050117852/NotificationEventPr0";


    static SQSHelper()
    {
        _amazonSQSClient = new AmazonSQSClient("REDACTED_AWS_KEY", "REDACTED_AWS_KEY");
        
        
    }
    public static async Task<bool> QueueEmailMessage(string from, string to, string subject, string content, string name)
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