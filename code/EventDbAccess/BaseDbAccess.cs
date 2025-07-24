using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

 namespace EventManagementDbAccess

{
    public class BaseDbAccess
    {
        protected readonly string? ConnectionString;
        protected readonly ILogger<BaseDbAccess> _logger;
        protected readonly double _cacheDurationInMinutes = 60;
        protected readonly IDistributedCache _cache;
        public BaseDbAccess(IConfiguration config, ILogger<BaseDbAccess> logger, IDistributedCache cache = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           // _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");
            }
            _logger.LogInformation("BaseDbAccess initialized.");
            if (config == null || string.IsNullOrWhiteSpace(config.GetConnectionString("Default")))
            {
                throw new ArgumentNullException(nameof(config), "Configuration or connection string cannot be null or empty.");
            }
            this.ConnectionString = config.GetConnectionString("Default");
            this._cache = cache;
        }
    }
}