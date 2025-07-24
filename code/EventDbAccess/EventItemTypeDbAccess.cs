
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventManagementDbAccess
{
    public class EventItemTypeDbAccess : BaseDbAccess
    {
        public EventItemTypeDbAccess(IConfiguration config, ILogger<EventItemTypeDbAccess> logger, IDistributedCache cache) : base(config, logger, cache)
        {
        }

        public async Task<int> CreateEventItemType(EventItemType item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO EventItemType 
                    (Name, Description, Cost, EventId, TotalAllowed, MaxPerOrder,CreatedAt) 
                    VALUES (@name, @description, @cost, @eventId, @totalAllowed, @maxPerOrder,@createdAt)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@eventId", item.EventId);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    _cache.RemoveCache<List<EventItemType>>(item.EventId.ToString());
                   // _cache.AddOrUpdateCache(item, item.EventItemTypeId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
                }   
                return rowsAffected > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating EventItemType: {ex.Message}");
                throw;
            }
        }

        // public async Task<EventItemType> GetEventItemTypeById(int eventItemTypeId)
        // {
        //     if (eventItemTypeId <= 0)
        //         throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(eventItemTypeId));

        //     string cacheKey = CacheHelper.GetCacheKey<EventItemType>(eventItemTypeId.ToString());
        //     EventItemType? cachedEvent = await _cache.GetOrSetAsync<EventItemType>(cacheKey, () => GetEventItemTypeByIdFromDb(eventItemTypeId), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
        //     return cachedEvent ?? throw new KeyNotFoundException($"Event with ID {eventItemTypeId} not found.") ;
        // }

        public async Task<EventItemType> GetEventItemTypeById(int eventItemTypeId)
        {
            if (eventItemTypeId <= 0)
                throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(eventItemTypeId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM EventItemType WHERE EventItemTypeId = @eventItemTypeId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventItemTypeId", eventItemTypeId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventItemType
                    {
                        EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        TotalAllowed = reader.GetInt32(reader.GetOrdinal("TotalAllowed")),
                        MaxPerOrder = reader.GetInt32(reader.GetOrdinal("MaxPerOrder"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving EventItemType: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EventItemType>> GetAllEventItemTypesByEventId(int eventId)
        {
            if (eventId <= 0)
                throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(eventId));

            string cacheKey = CacheHelper.GetCacheKey<List<EventItemType>>(eventId.ToString());
            List<EventItemType>? cachedEvent = await _cache.GetOrSetAsync(cacheKey, () => GetAllEventItemTypesByEventIdFromDb(eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return cachedEvent ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.") ;
        }

        public async Task<List<EventItemType>> GetAllEventItemTypesByEventIdFromDb(int eventId)
        {
            if (eventId <= 0)
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));
            var list = new List<EventItemType>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM EventItemType where EventId = @eventId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventId", eventId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new EventItemType
                    {
                        EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        TotalAllowed = reader.GetInt32(reader.GetOrdinal("TotalAllowed")),
                        MaxPerOrder = reader.GetInt32(reader.GetOrdinal("MaxPerOrder"))
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving EventItemTypes: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateEventItemType(EventItemType item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE EventItemType SET 
                    Name = @name,
                    Description = @description,
                    Cost = @cost,
                    EventId = @eventId,
                    TotalAllowed = @totalAllowed,
                    MaxPerOrder = @maxPerOrder,
                    ModifiedAt = @modifiedAt
                    WHERE EventItemTypeId = @eventItemTypeId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@eventId", item.EventId);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);
                cmd.Parameters.AddWithValue("@eventItemTypeId", item.EventItemTypeId);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    //remove all event item types from cache for the event. the next request will populate it.
                    //We could update the event item type in the cache, but it is better to remove it and let the next request populate it.
                    _cache.RemoveCache<List<EventItemType>>(item.EventId.ToString());
                    
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating EventItemType: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteEventItemType(int eventItemTypeId)
        {
            if (eventItemTypeId <= 0)
                throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(eventItemTypeId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM EventItemType WHERE EventItemTypeId = @eventItemTypeId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventItemTypeId", eventItemTypeId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {

                    //remove all event item types from cache for the event. the next request will populate it.
                    _cache.RemoveCache<List<EventItemType>>(eventItemTypeId.ToString());
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting EventItemType: {ex.Message}");
                throw;
            }
        }
    }
}