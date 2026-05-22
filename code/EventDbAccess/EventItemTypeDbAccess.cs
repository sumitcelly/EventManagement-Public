
using Amazon.Runtime.Internal.Util;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
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

                string query = @"INSERT INTO eventItemType 
                    (Name, Description, Cost, EventId, TotalAllowed, MaxPerOrder, SalesStartDate,SalesEndDate,
                    TicketValidityStart, TicketValidityEnd, AddOn, CreatedAt) 
                    VALUES (@name, @description, @cost, @eventId, @totalAllowed, @maxPerOrder,
                    @salesStartDate, @salesEndDate,@ticketValidityStart, @ticketValidityEnd, @addOn,@createdAt)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@eventId", item.EventId);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);
                cmd.Parameters.AddWithValue("@salesStartDate", item.SalesStartDate);
                cmd.Parameters.AddWithValue("@salesEndDate", item.SalesEndDate);
                cmd.Parameters.AddWithValue("@addOn", item.AddOn);
                cmd.Parameters.AddWithValue("@ticketValidityStart", item.TicketValidityStart);
                cmd.Parameters.AddWithValue("@ticketValidityEnd", item.TicketValidityEnd);

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

        
        public async Task<bool> UpdateTicketSoldCountInCache(int eventId, int eventItemTypeId, int quantity)
        {
            bool success = false;
            if (eventId<=0 || eventItemTypeId <= 0)
            {
                throw new ArgumentException($"Argument(s) are invalid");
            }
            try
            {               
                _logger.LogInformation($"{quantity} is available for evenitemtype {eventItemTypeId}.");
                string cacheKey = CacheHelper.GetCacheKey<List<EventItemType>>(eventId.ToString());
                _logger.LogInformation($"Found cache key{cacheKey}. Incrementing ticket sold by {quantity}");
                var data = await _cache.GetOnlyAsync<List<EventItemType>>(cacheKey);
                if (data != null)
                {
                    _logger.LogInformation($"Found item in cache with eventid {eventId}");
                    var itemType = data.FirstOrDefault(i => i.EventItemTypeId == eventItemTypeId);
                    if (itemType != null)
                    {
                        itemType.TicketsSold+=quantity;
                        await _cache.SetOnlyAsync(cacheKey, data);
                        _logger.LogInformation($"Updated tickets sold in cache to {itemType.TicketsSold} for event itemid {eventItemTypeId}");
                    }
                }                    
                    //_cache.RemoveCache<List<EventItemType>>(eventId.ToString());
              
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);

            }

            return success;
            
        }

        public async Task<EventItemType> GetEventItemTypeById(int eventItemTypeId)
        {
            if (eventItemTypeId <= 0)
                throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(eventItemTypeId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM eventItemType WHERE EventItemTypeId = @eventItemTypeId";
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
                        MaxPerOrder = reader.GetInt32(reader.GetOrdinal("MaxPerOrder")),
                        SalesStartDate = reader.IsDBNull(reader.GetOrdinal("SalesStartDate"))? DateTime.UtcNow:  reader.GetDateTime(reader.GetOrdinal("SalesStartDate")),
                        SalesEndDate = reader.IsDBNull(reader.GetOrdinal("SalesEndDate"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("SalesEndDate")),
                        TicketValidityStart = reader.IsDBNull(reader.GetOrdinal("TicketValidityStart"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("TicketValidityStart")),
                        TicketValidityEnd = reader.IsDBNull(reader.GetOrdinal("TicketValidityEnd"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("TicketValidityEnd")),
                       AddOn = !reader.IsDBNull(reader.GetOrdinal("AddOn")) && reader.GetBoolean(reader.GetOrdinal("AddOn")),

                        TicketsSold = reader.IsDBNull(reader.GetOrdinal("TicketsSold")) ? 0 : reader.GetInt32(reader.GetOrdinal("TicketsSold")),
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

                string query = "SELECT * FROM eventitemtype where EventId = @eventId";
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
                        MaxPerOrder = reader.GetInt32(reader.GetOrdinal("MaxPerOrder")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),

                        SalesStartDate = reader.IsDBNull(reader.GetOrdinal("SalesStartDate"))? DateTime.UtcNow:  reader.GetDateTime(reader.GetOrdinal("SalesStartDate")),
                        SalesEndDate = reader.IsDBNull(reader.GetOrdinal("SalesEndDate"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("SalesEndDate")),
                        TicketValidityStart = reader.IsDBNull(reader.GetOrdinal("TicketValidityStart"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("TicketValidityStart")),
                        TicketValidityEnd = reader.IsDBNull(reader.GetOrdinal("TicketValidityEnd"))? DateTime.UtcNow:reader.GetDateTime(reader.GetOrdinal("TicketValidityEnd")),
                        AddOn = !reader.IsDBNull(reader.GetOrdinal("AddOn")) && reader.GetBoolean(reader.GetOrdinal("AddOn")),

                        TicketsSold = reader.IsDBNull(reader.GetOrdinal("TicketsSold")) ? 0 : reader.GetInt32(reader.GetOrdinal("TicketsSold")),

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

        public async Task<bool> UpdateEventItemTypesSoldCount(int eventId, int itemType, int quantity,MySqlConnection connection,  MySqlTransaction transaction)
        {
            if (itemType <= 0)
                throw new ArgumentException("EventItemTypeId must be greater than zero.", nameof(itemType));

            try
            {

                string query = @"update eventmanagement.eventitemtype 
                        set ticketssold=ticketssold+@quantity,
                        ModifiedAt=@modifiedAt
                        where ticketssold+@quantity <= totalallowed and
                        eventitemtypeid=@itemType";
                _logger.LogInformation($"query for update count is:{query}");
                
                using (MySqlCommand cmd = new(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@itemType", itemType);
                    cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

                    int i = await cmd.ExecuteNonQueryAsync();
                    if (i <= 0)
                    {
                        _logger.LogWarning($"{quantity} ticket is  not available for evenitemtype {itemType}.");
                        throw new Exception($"Not enough tickets available for {itemType}");
                    }
                    await UpdateTicketSoldCountInCache(eventId, itemType, quantity);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error updating EventItemType sold count: {ex.Message}");
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

                string query = @"UPDATE eventitemtype SET 
                    Name = @name,
                    Description = @description,
                    Cost = @cost,
                    AddOn = @addOn,

                    TotalAllowed = @totalAllowed,
                    TicketsSold = @ticketsSold,
                    MaxPerOrder = @maxPerOrder,
                    SalesStartDate = @salesStartDate,
                    SalesEndDate = @salesEndDate,
                    TicketValidityStart = @ticketValidityStart,
                    TicketValidityEnd = @ticketValidityEnd,
                    ModifiedAt = @modifiedAt
                    WHERE EventItemTypeId = @eventItemTypeId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@addOn", item.AddOn);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@ticketsSold", item.TicketsSold);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);
                cmd.Parameters.AddWithValue("@eventItemTypeId", item.EventItemTypeId);
                cmd.Parameters.AddWithValue("@salesStartDate", item.SalesStartDate);
                cmd.Parameters.AddWithValue("@salesEndDate", item.SalesEndDate);
                cmd.Parameters.AddWithValue("@ticketValidityStart", item.TicketValidityStart);
                cmd.Parameters.AddWithValue("@ticketValidityEnd", item.TicketValidityEnd);
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

        public async Task<string> DeleteEventItemType(int eventId,int eventItemTypeId)
        {
            if (eventItemTypeId <= 0 || eventId <=0)
                throw new ArgumentException("EventItemTypeId or event Id must be greater than zero.");

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM eventitemtype WHERE EventItemTypeId = @eventItemTypeId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventItemTypeId", eventItemTypeId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {

                    //remove all event item types from cache for the event. the next request will populate it.
                    _cache.RemoveCache<List<EventItemType>>(eventId.ToString());
                }
                return rowsAffected > 0?string.Empty:"Unable to delete ticket type.";
            }
            catch (Exception ex)
            {
               _logger.LogError($"Error deleting EventItemType: {ex.Message}");
                return ex.Message.Contains("foreign key constraint") ? "Cannot delete ticket type if tickets has been sold for that type." : "An error occured when deleting ticket type.";
            }
        }
    }
}