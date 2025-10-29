using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventUtils;

 namespace EventManagementDbAccess
{
  public class EventDbAccess :BaseDbAccess
  {
    public EventDbAccess(IConfiguration configuration, ILogger<EventDbAccess> logger, IDistributedCache cache) :base(configuration, logger, cache)
    {
      
    }


    public async Task<List<EventHeader>> SearchEvents(string keyword, DateOnly startDate, int intervalDays,
                                                string city, string state, string category,
                                                int limit=10, DateTime cursor = default(DateTime))
    {
      // Implement search logic based on the provided parameters.
      // This is a placeholder implementation and should be replaced with actual search logic.
      List<EventHeader> events = new List<EventHeader>();
      using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
      {
        await connection.OpenAsync();
        {
          _logger.LogInformation("Connection to database established successfully.");
          string query = @" SELECT EventName,EventId,eventheadline,EventDescription,EventTags,EventOrganizer,
                            EventDate,EventAddress,EventCategory,Free,EventSummary,
                            MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) 
                            AGAINST (@keyword IN NATURAL LANGUAGE MODE) AS relevance
                            FROM Events 
                            WHERE 1=1";

          if (!string.IsNullOrEmpty(keyword))
          {
            query += @" AND MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) 
                        AGAINST (@keyword IN NATURAL LANGUAGE MODE)";
          }
          DateTime endDate = DateTime.MinValue;
          if (startDate != DateOnly.MinValue || intervalDays > 0)
          {
            if (intervalDays <= 0)
              intervalDays = 30; //default to 30 days
            endDate = startDate.AddDays(intervalDays).ToDateTime(new TimeOnly(23, 59, 59));
            query += " AND EventDate BETWEEN @start_date AND @end_date";
          }
          if (!string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(state))
          {
            query += " AND (City = @city OR State = @state)";
          }
          if (!string.IsNullOrEmpty(category))
          {
            query += " AND EventCategory = @category";
          }
          //the comparison means that some results maybe repeated.
          //So if there are multiple events at the same exact date and time, then
          //search results will show an overlap
          if (cursor != null)
          {
            query += " AND EventDate >= @cursor";
          }
          query += @" AND EventDate >= CURDATE() 
                                  ORDER BY EventDate ASC
                                  LIMIT @limit;";
          Console.WriteLine("Final Query: " + query);

          MySqlCommand cmd = new MySqlCommand(query, connection);
         
          cmd.Parameters.AddWithValue("@keyword",keyword ?? string.Empty);
          if (startDate != DateOnly.MinValue && intervalDays > 0)
          {
            cmd.Parameters.AddWithValue("@start_date", startDate.ToDateTime(new TimeOnly(0, 0, 0)));
            cmd.Parameters.AddWithValue("@end_date", endDate);
          }
         
          cmd.Parameters.AddWithValue("@city", city ?? string.Empty);
      
          cmd.Parameters.AddWithValue("@state", state ?? string.Empty);
          if (!string.IsNullOrEmpty(category))
            cmd.Parameters.AddWithValue("@category", category);
          cmd.Parameters.AddWithValue("@limit", limit);
          
          
           cmd.Parameters.AddWithValue("@cursor", cursor);
         

          using (MySqlDataReader reader = cmd.ExecuteReader())
            {
              while (reader.Read())
              {
                events.Add(new EventHeader()
                {
                  EventId = reader.GetInt32("EventId"),
                  EventName = reader.GetString("EventName"),
                  EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString("EventHeadline"),
                  EventDate = reader.GetDateTime("EventDate"),
                  EventOrganizerId = reader.GetInt32("EventOrganizer"),
                  EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary")) ? string.Empty : reader.GetString("EventSummary"),
                  Free = reader.GetBoolean("Free"),
                  EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString("EventAddress")
                });
              }
            }
        }
      }
      return events;
    }
    
  
    public async Task<EventHeader> GetEventHeaderById(int eventId)
    {
      if (eventId <= 0)
          throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      string cacheKey = CacheHelper.GetCacheKey<EventHeader>(eventId.ToString());
      EventHeader? cachedEvent = await _cache.GetOrSetAsync(cacheKey, () => GetEventHeaderByIdFromDb(eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
      return cachedEvent ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.") ;
    }

    public async Task<EventHeader> GetEventHeaderByIdFromDb(int eventId)
    {
      if (eventId <= 0)
        throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        var query = @"select a.EventId,a.EventName,a.EventHeadline,a.EventDate,
                    a.EventOrganizer,  a.EventSummary,a.Free,
                    ifnull(a.EventAddress,'') as EventAddress,
                    b.OrganizerName from events a, eventorganizer b 
                    WHERE a.EventOrganizer= b.CustomerId and 
                    a.EventId = @eventId";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return new EventHeader
          {
            EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
            EventName = reader.GetString(reader.GetOrdinal("EventName")),
            EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline"))?string.Empty: reader.GetString(reader.GetOrdinal("EventHeadline")),
            EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
            //EventOrganizer = reader.GetString(reader.GetOrdinal("EventOrganizer")),
            EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary"))?string.Empty: reader.GetString(reader.GetOrdinal("EventSummary")),

            Free = reader.GetBoolean(reader.GetOrdinal("Free")),
            EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
            EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
          };
        }
      }
      return null;
    }
    
    public async Task<Event> GetEventDetailsById(int eventId)
    {
      if (eventId <= 0)
          throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      string cacheKey = CacheHelper.GetCacheKey<Event>(eventId.ToString());
      Event? cachedEvent = await _cache.GetOrSetAsync(cacheKey, () => GetEventDetailsByIdFromDb(eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
      return cachedEvent ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.") ;
    }

    public async Task<Event> GetEventDetailsByIdFromDb(int eventId)
    {
      if (eventId <= 0)
        throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        var query = @"select * from events  
                    WHERE
                    EventId = @eventId";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return new Event
          {
            EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
            EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
            EventName = reader.GetString(reader.GetOrdinal("EventName")),
            EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventHeadline")),
            EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
            EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventSummary")),
            Free = reader.GetBoolean(reader.GetOrdinal("Free")),
            Duration = reader.GetInt16(reader.GetOrdinal("Duration")),
            EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress")),
            EventDescription = reader.IsDBNull(reader.GetOrdinal("EventDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventDescription")),
            Category = reader.IsDBNull(reader.GetOrdinal("EventCategory")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventCategory")),
            SubCategory = reader.IsDBNull(reader.GetOrdinal("SubCategory")) ? string.Empty : reader.GetString(reader.GetOrdinal("SubCategory")),
            Tags = reader.IsDBNull(reader.GetOrdinal("EventTags")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventTags")),
            EventAgenda = reader.IsDBNull(reader.GetOrdinal("EventAgenda")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAgenda")),
            Capacity = reader.IsDBNull(reader.GetOrdinal("Capacity")) ? 0 : reader.GetInt32(reader.GetOrdinal("Capacity")),
            StreetAddress = reader.IsDBNull(reader.GetOrdinal("StreetAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("StreetAddress")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
            State = reader.IsDBNull(reader.GetOrdinal("State")) ? string.Empty : reader.GetString(reader.GetOrdinal("State")),
            ZipCode = reader.IsDBNull(reader.GetOrdinal("ZipCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ZipCode")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? string.Empty : reader.GetString(reader.GetOrdinal("Country")),
            Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Latitude")),
            Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Longitude")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
          };
        }
      }
      return null;
    }

    //Cannot cache by name since name can be changed and theb the cache wil retain the old name.
    //We need to send the old name to the cache and then remove it when the event is updated.

    // public async Task<Event> GetEventByName(string eventName)
    // {
    //   if (string.IsNullOrEmpty(eventName))
    //     throw new ArgumentNullException(nameof(eventName));
    //   string cacheKey = CacheHelper.GetCacheKey<Event>(eventName);

    //   Event? cachedEvent = await _cache.GetOrSetAsync<Event>(cacheKey, () => GetEventByNameFromDb(eventName), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
    //   return cachedEvent ?? throw new KeyNotFoundException($"Event with name {eventName} not found.") ;
    // }

    // public async Task<Event> GetEventByName(string eventName)
    // {
    //   if (string.IsNullOrEmpty(eventName))
    //     throw new ArgumentNullException(nameof(eventName));

    //   using var conn = new MySqlConnection(this.ConnectionString);
    //   await conn.OpenAsync();
    //   var query = "SELECT * FROM Events WHERE EventName = @eventName";

    //   using var cmd = new MySqlCommand(query, conn);
    //   cmd.Parameters.AddWithValue("@eventName", eventName);

    //   using var reader = await cmd.ExecuteReaderAsync();
    //   if (await reader.ReadAsync())
    //   {
    //     return new Event
    //     {
    //       EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
    //       EventName = reader.GetString(reader.GetOrdinal("EventName")),
    //       EventDescription = reader.GetString(reader.GetOrdinal("EventDescription")),
    //       EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
    //       EventOrganizer = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
    //       EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
    //     };
    //   }
    //   return null;
    // }

    public async Task<int> CreateEvent(Event evt)
    {
      if (evt == null)
        throw new ArgumentNullException(nameof(evt));

      using var connection = new MySqlConnection(this.ConnectionString);
      await connection.OpenAsync();

      string query = @"INSERT INTO Events 
            (EventName,EventHeadline, EventDescription, EventDate, Duration,
            EventOrganizer, EventAddress,EventAgenda, EventTags,IsLive,Private,
            Latitude,Longitude,StreetAddress,City,State,ZipCode,
            CreatedAt) 
            VALUES (@name,@headline, @desc, @date, @duration,
             @organizer, @location, @agenda, @tags, @isLive, @isPrivate,
             @lat,@long,@streetAddress, @city, @state, @zipCode, @createdAt)";

      using var cmd = new MySqlCommand(query, connection);
      cmd.Parameters.AddWithValue("@name", evt.EventName);
      cmd.Parameters.AddWithValue("@headline", evt.EventHeadline);
      cmd.Parameters.AddWithValue("@desc", evt.EventDescription);
      cmd.Parameters.AddWithValue("@date", evt.EventDate);
      cmd.Parameters.AddWithValue("@duration", evt.Duration);

      cmd.Parameters.AddWithValue("@organizer", evt.EventOrganizerId);
      cmd.Parameters.AddWithValue("@location", evt.EventLocation);
      cmd.Parameters.AddWithValue("@agenda", evt.EventAgenda);
      cmd.Parameters.AddWithValue("@tags", evt.Tags);
      cmd.Parameters.AddWithValue("@isLive", evt.IsLive);
      cmd.Parameters.AddWithValue("@isPrivate", evt.IsPrivate);

      cmd.Parameters.AddWithValue("@lat", evt.Latitude);
      cmd.Parameters.AddWithValue("@long", evt.Longitude);
      cmd.Parameters.AddWithValue("@streetAddress", evt.StreetAddress);
      cmd.Parameters.AddWithValue("@city", evt.City);
      cmd.Parameters.AddWithValue("@state", evt.State);
      cmd.Parameters.AddWithValue("@zipCode", evt.ZipCode);

      cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

      int rowsAffected = await cmd.ExecuteNonQueryAsync();
      _cache.AddOrUpdateCache(evt, evt.EventId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));

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
        if (rowsAffected > 0)
        {
            _cache.RemoveCache<Event>(eventId.ToString());
        }
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
            EventHeadline = @headline,
            EventDescription = @desc,
            EventAgenda = @agenda,
            EventTags = @tags,
            EventDate = @date,
            Duration =@duration,
            EventAddress = @location,
            Latitude = @lat,
            Longitude =@long,
            StreetAddress = @streetAddress,
            City = @city,
            State = @state,
            ZipCode =@zipCode,
            IsLive = @isLive,
            Private = @isPrivate,
            ModifiedAt = @modifiedAt
            WHERE EventId = @eventId";

      using var cmd = new MySqlCommand(query, connection);
 
      cmd.Parameters.AddWithValue("@name", evt.EventName);
      cmd.Parameters.AddWithValue("@headline", evt.EventHeadline);
      cmd.Parameters.AddWithValue("@desc", evt.EventDescription);
      cmd.Parameters.AddWithValue("@date", evt.EventDate);
      cmd.Parameters.AddWithValue("@duration", evt.Duration);

      cmd.Parameters.AddWithValue("@organizer", evt.EventOrganizerId);
      cmd.Parameters.AddWithValue("@location", evt.EventLocation);
      cmd.Parameters.AddWithValue("@agenda", evt.EventAgenda);
      cmd.Parameters.AddWithValue("@tags", evt.Tags);
      cmd.Parameters.AddWithValue("@isLive", evt.IsLive);
      cmd.Parameters.AddWithValue("@isPrivate", evt.IsPrivate);

      cmd.Parameters.AddWithValue("@lat", evt.Latitude);
      cmd.Parameters.AddWithValue("@long", evt.Longitude);
      cmd.Parameters.AddWithValue("@streetAddress", evt.StreetAddress);
      cmd.Parameters.AddWithValue("@city", evt.City);
      cmd.Parameters.AddWithValue("@state", evt.State);
      cmd.Parameters.AddWithValue("@zipCode", evt.ZipCode);
      cmd.Parameters.AddWithValue("@eventId", evt.EventId);
      cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected > 0)
        { 
           _cache.AddOrUpdateCache(evt, evt.EventId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
        }
        return rowsAffected > 0;
    }

  }
}
