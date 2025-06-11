using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventDbAccess
{
  public class EventContext
  {
    public string ConnectionString { get; set; }

    private MySqlConnection mySqlConnection {get;}
    public EventContext(string connectionString)
    {
      this.ConnectionString = connectionString;
      this.mySqlConnection = new MySqlConnection(connectionString); 
    }

    private MySqlConnection GetConnection()
    {
      if (mySqlConnection.State != System.Data.ConnectionState.Open)
          mySqlConnection.Open();
        
      return mySqlConnection;
    
    }

    public List<Event> GetAllEvents()
    {
      List<Event> list = new List<Event>();

      MySqlConnection conn = GetConnection();
      {
    
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Events", conn);
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
              EventLocation = reader.IsDBNull(reader.GetOrdinal("EventLocation")) ? string.Empty : reader.GetString("EventLocation")
            });
          }
        }
      }

      return list;
    }
  
    public async Task<Event> GetEventById(int eventId)
    {
        using var conn = GetConnection();
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
        return null;
    }

    public async Task<Event> GetEventByName(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            throw new ArgumentNullException(nameof(eventName));

        using var conn = GetConnection();
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
              EventLocation = reader.IsDBNull(reader.GetOrdinal("EventLocation")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventLocation"))
            };
        }
        return null;
    }

  }
}
