
namespace EventManagementDbAccess;

public class TicketStatusCount
{
    public required string TicketStatus {get;set;}
    public int StatusCount {get;set;}
}

public class TicketDaySales
{
    public required DateTime SalesDate {get;set;}

    public int TicketsSold {get;set;}
}