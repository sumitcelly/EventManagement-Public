namespace ScratchLambda;

public class NotficationMessage
{
    public required string Name { get; set; }

    public required string To { get; set; }

    public string Sms { get; set; } = string.Empty;

    public required string From { get; set; }

    public required string Subject { get; set; }
    
    public required string Body { get; set; }

    public  int RefID { get; set; }

    public required  string MessageType { get; set; } 

}