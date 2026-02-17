using System.Security.Claims;
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;

public class OwnerRequirement : IAuthorizationRequirement
{
    public string ParameterName { get; }
    public OwnerRequirement(string parameterName) => ParameterName = parameterName;
}

public class GenericOwnerHandler  : AuthorizationHandler<OwnerRequirement>
{
    private readonly EventDbAccess _eventDbContext;

    private readonly SalesOrderDbAccess _salesOrderDbAccess;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly ILogger<GenericOwnerHandler> _logger;
    public GenericOwnerHandler(EventDbAccess eventDbAccess, 
                                SalesOrderDbAccess salesOrderDbAccess,
                                IHttpContextAccessor httpContextAccessor, 
                                ILogger<GenericOwnerHandler> logger)
    {
        _eventDbContext = eventDbAccess;
        _httpContextAccessor = httpContextAccessor;
        _salesOrderDbAccess = salesOrderDbAccess;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        OwnerRequirement requirement)
    {
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("HTTP context is null in authorization handler.");
            return;
        }

        _logger.LogInformation($"Authorization handler invoked for parameter: {requirement.ParameterName}");
        // 1. Get customer ID from JWT claims
        var customerIdFromJwt = context.User.FindFirst("CustomerId")?.Value;

        var userIdFromJwt = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
       
        if (string.IsNullOrEmpty(customerIdFromJwt) || string.IsNullOrEmpty(userIdFromJwt) || 
                !int.TryParse(customerIdFromJwt, out int customerId) || 
                !int.TryParse(userIdFromJwt, out int userId))
        {  
            _logger.LogWarning($"Authorization failed: Missing or invalid customerId or userid. customerId: {customerIdFromJwt}, userId: {userIdFromJwt}");          
            return;
        }

        
        // 1. Find the ID value in Route Data or Query String using the dynamic name
        var idValue = httpContext.GetRouteValue(requirement.ParameterName)?.ToString() 
                    ?? httpContext.Request.Query[requirement.ParameterName].ToString();
        if (string.IsNullOrEmpty(idValue) || !int.TryParse(idValue, out int targetId))
        {
            _logger.LogError($"Invalid or missing target ID in request. Parameter: {requirement.ParameterName}, Value: {idValue}");
            return;
        }

        bool isOwner = false;
        switch (requirement.ParameterName)
        {
            case "eventId":
            {
                EventHeader header = await _eventDbContext.GetEventHeaderById(targetId);
                //check if the event exists and the organizer id matches the customer id from JWT
                if (header != null
                    && header.EventOrganizerId == customerId)
                {
                    isOwner = true;
                    _logger.LogInformation($"Authorization check passed: User is the organizer of the event. eventId: {targetId}, customerId: {customerId}");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Event not found or user is not the organizer. eventId: {targetId}, customerId: {customerId}");
                }
                break;
            }
            case "orderId":
            {
                isOwner = await _salesOrderDbAccess.VerifySalesOrderUser(targetId,userId);
                break;
            }
            default:
                _logger.LogError($"Unsupported parameter for ownership check: {requirement.ParameterName}");
                    return;
        }
            
        if (isOwner)
        {
            _logger.LogInformation($"Authorization succeeded: User/customer is the owner based on parameter {requirement.ParameterName} with value {targetId}.");
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning($"Authorization failed: User/customer is not the owner based on parameter {requirement.ParameterName} with value {targetId}.");
            return;
        }
    }
}
