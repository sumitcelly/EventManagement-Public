using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using System.Text.Json;
using System.Buffers.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScratchLambda;

public class Function
{

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 
    public async Task<List<SQSBatchResponse.BatchItemFailure>> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        List<SQSBatchResponse.BatchItemFailure> batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();
        NotificationSender.Init(context.Logger);

        foreach (var message in evnt.Records)
        {
            bool success = await ProcessMessageAsync(message, context);
            if (!success)
            {
                context.Logger.LogError("Batch has errors");
                batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure() { ItemIdentifier = message.MessageId });
            }
        }
        return batchItemFailures;    
    }

    /*
    {"Name":"Hello","To":"scelly@securevideo.com","Subject":"Hello","FromAddress":"support@eventspro.com",
        "Body":"PGh0bWw+PGJvZHk+PGgyPkhlbGxvIGZyb20gQW1hem9uIFNFUzwvaDI+PHVsPjxsaT5JJ20gYSBsaXN0IGl0ZW08L2xpPjxsaT5TbyBhbSBJITwvbGk+PC9ib2R5PjwvaHRtbD4="
        }
    */
    private async Task<bool> ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        bool processingComplete = false;
        try
        {
            if (string.IsNullOrWhiteSpace(message.Body))
            {
                context.Logger.LogError("No message body received! Skippig message processing." + message.MessageId);
                //true because there is not point retrying the message
                return true;
            }

            NotficationMessage msgBody = JsonSerializer.Deserialize<NotficationMessage>(message.Body);
            if (msgBody == null || string.IsNullOrEmpty(msgBody.To) || string.IsNullOrEmpty(msgBody.Subject) ||
                string.IsNullOrEmpty(msgBody.Body) || string.IsNullOrEmpty(msgBody.From))
            {
                context.Logger.LogError($"Message for ID {message.MessageId} is missing data in body {message.Body}");
                //true because there is not point retrying the message
                return true;
            }

            NotificationResponse sendResult = await NotificationSender.SendEmail(msgBody.To, msgBody.Subject,
                                                    System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(msgBody.Body)),
                                                     msgBody.From);

            if (!string.IsNullOrWhiteSpace(sendResult.MessageId))
            {
                context.Logger.LogInformation($"Processed message sent to {msgBody.Name} with email {msgBody.To} successfully with message id {message.MessageId}");
                processingComplete = true;
            }
            else
            {
                context.Logger.LogInformation($@"Failed to send  to {msgBody.Name} with email {msgBody.To} due to error code {sendResult.ErrorCode}
                                                and error message {sendResult.ErrorMessage} ");
                processingComplete = !sendResult.RetryOnError;
            }

        }
        catch (ArgumentNullException exc)
        {
            context.Logger.LogError($"Argument null excpetion thrown: {exc.Message})");
            processingComplete = true;
        }
        catch (Exception e)
        {
            //You can use Dead Letter Queue to handle failures. By configuring a Lambda DLQ.
            context.Logger.LogError($"An error occurred {e.Message}");
            //may have to reverse this based on the error
            processingComplete  = true;
        }

        return processingComplete;
    }
}