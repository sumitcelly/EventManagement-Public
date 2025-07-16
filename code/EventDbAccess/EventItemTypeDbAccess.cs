
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
        public EventItemTypeDbAccess(IConfiguration config, ILogger<EventItemTypeDbAccess> logger) : base(config, logger)
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
                    (Name, Description, Cost, EventId, TotalAllowed, MaxPerOrder) 
                    VALUES (@name, @description, @cost, @eventId, @totalAllowed, @maxPerOrder)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@eventId", item.EventId);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating EventItemType: {ex.Message}");
                throw;
            }
        }

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
                    MaxPerOrder = @maxPerOrder
                    WHERE EventItemTypeId = @eventItemTypeId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@cost", item.Cost);
                cmd.Parameters.AddWithValue("@eventId", item.EventId);
                cmd.Parameters.AddWithValue("@totalAllowed", item.TotalAllowed);
                cmd.Parameters.AddWithValue("@maxPerOrder", item.MaxPerOrder);
                cmd.Parameters.AddWithValue("@eventItemTypeId", item.EventItemTypeId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
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