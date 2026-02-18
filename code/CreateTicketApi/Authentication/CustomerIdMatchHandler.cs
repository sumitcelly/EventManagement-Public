
using Microsoft.AspNetCore.Authorization;

public class CustomerIdMatchRequirement : IAuthorizationRequirement { }

public class CustomerIdMatchHandler : AuthorizationHandler<CustomerIdMatchRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomerIdMatchHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomerIdMatchRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;

        // 1. Get the ID from the JWT (adjust "customerId" to match your token's claim key)
        var jwtId = context.User.FindFirst("CustomerId")?.Value;
                    

        // 2. Hardcoded lookup for "customerId" in the query string (?customerId=xxx)
        var queryId = httpContext.GetRouteValue("customerId")?.ToString();

        // 3. Comparison
        if (!string.IsNullOrEmpty(jwtId) && jwtId.Equals(queryId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
