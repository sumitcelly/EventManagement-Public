using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

 namespace EventManagementDbAccess
{
  public class EventDbAccess :BaseDbAccess
  {
    public EventDbAccess(IConfiguration configuration, ILogger<EventDbAccess> logger) :base(configuration, logger)
    {
      
    }
   

    public async Task<List<Event>> GetAllEvents()
    {
      List<Event> list = new List<Event>();

      using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
      {
        await connection.OpenAsync();
        {
          _logger.LogInformation("Connection to database established successfully.");
          MySqlCommand cmd = new MySqlCommand("SELECT * FROM Events", connection);
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              list.Add(new Event()
              {
                EventId = reader.GetInt32("EventId"),
                EventName = reader.GetString("EventName"),
                EventDescription = reader.GetString("EventDescription"),
                EventDate = reader.GetDateTime("EventDate"),
                EventOrganizer = reader.GetInt32("EventOrganizer"),
                EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString("EventAddress")
              });
            }
          }
        }
      }
      return list;
    }
  
    public async Task<Event> GetEventById(int eventId)
    {
      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        var query = "SELECT * FROM Events WHERE EventId = @eventId";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return new Event
          {
            EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
            EventName = reader.GetString(reader.GetOrdinal("EventName")),
            EventDescription = reader.GetString(reader.GetOrdinal("EventDescription")),
            EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
            EventOrganizer = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
            EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
          };
        }
      }
      return null;
    }

    public async Task<Event> GetEventByName(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            throw new ArgumentNullException(nameof(eventName));

        using var conn = new MySqlConnection(this.ConnectionString);
        await conn.OpenAsync();
        var query = "SELECT * FROM Events WHERE EventName = @eventName";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventName", eventName);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Event
            {
              EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
              EventName = reader.GetString(reader.GetOrdinal("EventName")),
              EventDescription = reader.GetString(reader.GetOrdinal("EventDescription")),
              EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
              EventOrganizer = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
              EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
            };
        }
        return null;
    }

    public async Task<int> CreateEvent(Event evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = @"INSERT INTO Events 
            (EventName, EventDescription, EventDate, EventOrganizer, EventAddress,CreatedAt) 
            VALUES (@name, @desc, @date, @organizer, @location, @createdAt)";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@name", evt.EventName);
        cmd.Parameters.AddWithValue("@desc", evt.EventDescription);
        cmd.Parameters.AddWithValue("@date", evt.EventDate);
        cmd.Parameters.AddWithValue("@organizer", evt.EventOrganizer);
        cmd.Parameters.AddWithValue("@location", evt.EventLocation);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
    }

    public async Task<bool> DeleteEvent(int eventId)
    {
        if (eventId <= 0)
            throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = "DELETE FROM Events WHERE EventId = @eventId";
        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateEvent(Event evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = @"UPDATE Events SET 
            EventName = @name,
            EventDescription = @desc,
            EventDate = @date,
            EventOrganizer = @organizer,
            EventAddress = @location,
            ModifiedAt = @modifiedAt
            WHERE EventId = @eventId";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@name", evt.EventName);
        cmd.Parameters.AddWithValue("@desc", evt.EventDescription);
        cmd.Parameters.AddWithValue("@date", evt.EventDate);
        cmd.Parameters.AddWithValue("@organizer", evt.EventOrganizer);
        cmd.Parameters.AddWithValue("@location", evt.EventLocation);
        cmd.Parameters.AddWithValue("@eventId", evt.EventId);
        cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

  }
}
