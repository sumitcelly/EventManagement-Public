using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EventUtils;

namespace EventManagementDbAccess
{
    public class EventFeeOverride
    {
        public int EventId { get; set; }
        public decimal CustomPercentage { get; set; }
    }

    public class EventOverrideBaseDbAccess : BaseDbAccess
    {
        public EventOverrideBaseDbAccess(IConfiguration configuration, ILogger<EventOverrideBaseDbAccess> logger, IDistributedCache cache) : base(configuration, logger, cache)
        {
        }

        /// <summary>
        /// Returns the fee override for an event. Uses redis cache when available.
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public async Task<EventFeeOverride?> GetEventFeeOverride(int eventId)
        {
            if (eventId <= 0)
                return null;
                
            string cacheKey = CacheHelper.GetCacheKey<EventFeeOverride>(eventId.ToString());

            EventFeeOverride? cached = await _cache.GetOrSetAsync<EventFeeOverride>(cacheKey, () => GetEventFeeOverrideFromDb(eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return cached;
        }

        public async Task<EventFeeOverride?> GetEventFeeOverrideFromDb(int eventId)
        {
            if (eventId <= 0)
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

            try
            {
                using var connection = new MySqlConnection(this.ConnectionString);
                await connection.OpenAsync();
                var query = @"SELECT EventId, CustomPercentage FROM eventmanagement.eventfeeoverrides WHERE EventId = @eventId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventId", eventId);

                using DbDataReader reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                while (await reader.ReadAsync())
                {
                    return new EventFeeOverride
                    {
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        CustomPercentage = reader.GetDecimal(reader.GetOrdinal("CustomPercentage"))
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading fee override for event {eventId}: {ex.Message}");
                throw;
            }
            return null;
        }
    }
}
