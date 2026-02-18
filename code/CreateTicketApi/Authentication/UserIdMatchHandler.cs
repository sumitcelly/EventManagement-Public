
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public class UserIdMatchRequirement : IAuthorizationRequirement { }

public class UserIdMatchHandler : AuthorizationHandler<UserIdMatchRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdMatchHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserIdMatchRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;

        // 1. Get the ID from the JWT (adjust "UserId" to match your token's claim key)
        var jwtId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        

        // 2. Hardcoded lookup for "UserId" in the query string (?UserId=xxx)
        var queryId = httpContext.GetRouteValue("userId")?.ToString();

        // 3. Comparison
        if (!string.IsNullOrEmpty(jwtId) && jwtId.Equals(queryId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
