using System.Security.Claims;
using EventManagementDbAccess;
using Microsoft.AspNetCore.Authorization;

public record AtleastRoleRequirement(UserRoles minimumRole) : IAuthorizationRequirement;

public class AtleastRoleHandler : AuthorizationHandler<AtleastRoleRequirement>
{
    private readonly ILogger<AtleastRoleHandler> _logger;

    public AtleastRoleHandler(ILogger<AtleastRoleHandler> logger)
    {
        _logger = logger;
    }
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
                                                AtleastRoleRequirement requirement)
                                               
    {
        
        // Try to find the role claim. 
        // If you used JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(), use "role".
        // Otherwise, ClaimTypes.Role is usually mapped to the long XML schema URI.
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value 
                        ?? context.User.FindFirst("role")?.Value;
        _logger.LogInformation($"AtleastRoleHandler invoked. User role claim value: '{roleClaim}'. Minimum required role: {requirement.minimumRole}.");
        if (string.IsNullOrEmpty(roleClaim))
        {
            _logger.LogWarning("Authorization failed: No role claim found in the user's claims.");
            return Task.CompletedTask;
        }

        // Use 'true' to ignore case when parsing the string "FullAdmin"
        if (Enum.TryParse<UserRoles>(roleClaim, true, out var userRole))
        {
            if (userRole >= requirement.minimumRole)
            {
                context.Succeed(requirement);
                _logger.LogInformation($"Authorization succeeded for role {userRole} which meets the minimum requirement of {requirement.minimumRole}.");
            }
            else
            {
                _logger.LogWarning($"Authorization failed: User role {userRole} does not meet the minimum requirement of {requirement.minimumRole}.");
                return Task.CompletedTask;
            }
        }
        else
        {
            _logger.LogWarning($"Authorization failed: Unable to parse user role from claim value '{roleClaim}'.");
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}
