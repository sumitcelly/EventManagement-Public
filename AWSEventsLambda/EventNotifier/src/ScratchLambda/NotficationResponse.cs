namespace ScratchLambda;

public class NotificationResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty ;
    public string MessageId { get; set; } = string.Empty;

    public bool RetryOnError { get; set; } = true;
}