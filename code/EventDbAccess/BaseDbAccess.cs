using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

 namespace EventManagementDbAccess

{
    public class BaseDbAccess
    {
        protected readonly string? ConnectionString;
        protected readonly ILogger<BaseDbAccess> _logger;
        public BaseDbAccess(IConfiguration config, ILogger<BaseDbAccess> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("BaseDbAccess initialized.");
            if (config == null || string.IsNullOrWhiteSpace(config.GetConnectionString("Default")))
            {
                throw new ArgumentNullException(nameof(config), "Configuration or connection string cannot be null or empty.");
            }
            this.ConnectionString = config.GetConnectionString("Default");
        }
    }
}