using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

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
              EventOrganizer = reader.GetString("EventOrganizer")
            });
          }
        }
      }

      return list;
    }
  
  }
}
