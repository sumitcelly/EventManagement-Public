using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventUtils;
using Stripe.TestHelpers.Terminal;
using System.Text.RegularExpressions;
using System.Data.Common;
using ZstdSharp;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;

namespace EventManagementDbAccess
{
  public class EventDbAccess :BaseDbAccess
  {
    private readonly int eventHeaderCacheTime =60;
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
          string query = @" SELECT EventName,EventId,EventUrlName, Latitude, Longitude, RefundMode, TicketFeeDisplayMode, eventheadline,EventDescription,EventTags,EventOrganizer,
                            EventDate,EventAddress,EventCategory,Free,EventSummary,EventBannerFileName,b.OrganizerEventBaseUrl,
                            MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) 
                            AGAINST (@keyword IN NATURAL LANGUAGE MODE) AS relevance
                            FROM events 
                            inner join eventorganizer b on b.CustomerId=events.eventorganizer
                            WHERE IsLive=1";

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
                  Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude")) ? 0m : reader.GetDecimal("Latitude"),
                  Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude")) ? 0m : reader.GetDecimal("Longitude"),
                  RefundMode = reader.IsDBNull(reader.GetOrdinal("RefundMode"))?0: (RefundMode)Enum.Parse(typeof(RefundMode), reader.GetString(reader.GetOrdinal("RefundMode"))),
                  TicketFeeMode = reader.IsDBNull(reader.GetOrdinal("TicketFeeDisplayMode"))?0: (TicketFeeMode)Enum.Parse(typeof(TicketFeeMode), reader.GetString(reader.GetOrdinal("TicketFeeDisplayMode"))),  
                  EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))?
                               StringUtils.CreateUrlSlug(reader.GetString("EventName")):
                               reader.GetString("EventUrlName"),
                  OrganizerUrlName = reader.IsDBNull(reader.GetOrdinal("OrganizerEventBaseUrl"))?
                                  string.Empty:
                               reader.GetString("OrganizerEventBaseUrl"),
                  EventBannerUrl= reader.IsDBNull(reader.GetOrdinal("EventBannerFileName")) ? string.Empty : 
                                AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString("EventBannerFileName")),
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

      //Maybe we need to get this from the event cache. too many caches here. (eventheaderbyid, eventbyname, eventbyid)
      string cacheKey = CacheHelper.GetCacheKey<EventHeader>(eventId.ToString());
      EventHeader? cachedEvent = await _cache.GetOrSetAsync(cacheKey, () => GetEventHeaderByIdFromDb(eventId), TimeSpan.FromMinutes(eventHeaderCacheTime), _logger);
      return cachedEvent ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.") ;
    }

    public async Task<EventHeader> GetEventHeaderByIdFromDb(int eventId)
    {
      if (eventId <= 0)
        throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        var query = @"select a.EventId,a.EventName,a.Duration,a.EventUrlName,
                    a.RefundMode, a.TicketFeeDisplayMode, a.EventHeadline,
                    a.EventDate, a.EventBannerFileName,
                    a.EventOrganizer,  a.EventSummary,a.Free,
                    ifnull(a.EventAddress,'') as EventAddress,a.Latitude,a.Longitude,
                    b.OrganizationName, b.OrganizerEventBaseUrl
                    from events a 
                    inner join eventorganizer b on b.CustomerId=a.eventorganizer
                    where a.EventId = @eventId";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return new EventHeader
          {
            EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
            EventName = reader.GetString(reader.GetOrdinal("EventName")),
            RefundMode = reader.IsDBNull(reader.GetOrdinal("RefundMode"))?0: (RefundMode)Enum.Parse(typeof(RefundMode), reader.GetString(reader.GetOrdinal("RefundMode"))),
            TicketFeeMode = reader.IsDBNull(reader.GetOrdinal("TicketFeeDisplayMode"))?0: (TicketFeeMode)Enum.Parse(typeof(TicketFeeMode), reader.GetString(reader.GetOrdinal("TicketFeeDisplayMode"))),  

            EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))
                               ?StringUtils.CreateUrlSlug(reader.GetString(reader.GetOrdinal("EventName")))
                               :reader.GetString(reader.GetOrdinal("EventUrlName")),
            EventBannerUrl= reader.IsDBNull(reader.GetOrdinal("EventBannerFileName")) ? string.Empty : 
                                AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString(reader.GetOrdinal("EventBannerFileName"))),
            EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventHeadline")),
            EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
            Duration = reader.GetInt16(reader.GetOrdinal("Duration")),
            OrganizerUrlName =  reader.IsDBNull(reader.GetOrdinal("OrganizerEventBaseUrl")) ? string.Empty: 
                                reader.GetString(reader.GetOrdinal("OrganizerEventBaseUrl")),
            EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventSummary")),
            Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Latitude")),
            Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Longitude")),

            Free = reader.GetBoolean(reader.GetOrdinal("Free")),
            EventOrganizer = reader.IsDBNull(reader.GetOrdinal("OrganizationName")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizationName")),
            EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
            EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
          };
        }
      }
      return null;
    }

    public async Task<EventSettings> GetEventSettings(int eventId)
    {
      if (eventId <= 0)
        throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

   
        var query = @"select a.IsLive, a.EventName,a.EventUrlName, a.RefundMode,a.TicketFeeDisplayMode,
                    (Select count(*) from eventitemtype b where b.eventid = a.eventid) AS tickettypecount
                    FROM events a WHERE a.eventid = @eventId";
                    
                   
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return new EventSettings
          {
            RefundMode = reader.IsDBNull(reader.GetOrdinal("RefundMode"))?0: (RefundMode)Enum.Parse(typeof(RefundMode), reader.GetString(reader.GetOrdinal("RefundMode"))),
            TicketFeeMode = reader.IsDBNull(reader.GetOrdinal("TicketFeeDisplayMode"))?0: (TicketFeeMode)Enum.Parse(typeof(TicketFeeMode), reader.GetString(reader.GetOrdinal("TicketFeeDisplayMode"))),  
            IsLive = reader.GetBoolean(reader.GetOrdinal("IsLive")),
            TicketStatus = reader.GetInt16(reader.GetOrdinal("tickettypecount")) > 0 ? true : false,
            EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))
                               ?StringUtils.CreateUrlSlug(reader.GetString(reader.GetOrdinal("EventName")))
                               :reader.GetString(reader.GetOrdinal("EventUrlName")),
          };
        }
      }
      return null;
    }

    
    
    public async Task<List<EventHeader>> GetEventListByCustomerId(int customerId,bool? isLive, bool? includePastEvents)
    {
      if (customerId <= 0)
        throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        //todo: maybe get all events including past events 
        var query = @"select a.EventId,a.EventName,a.EventUrlName,a.RefundMode,a.TicketFeeDisplayMode,
                    a.EventHeadline,a.EventDate, a.EventBannerFileName,
                    a.EventOrganizer,  a.EventSummary,a.Free,
                    ifnull(a.EventAddress,'') as EventAddress,
                    a.IsLive,a.Duration
                    from events a
                    WHERE a.EventOrganizer= @customerId and a.EventDate>=@eventDate";
        
        if (isLive.HasValue)
        {
          query+=" and a.isLive = @isLive";      
          _logger.LogInformation($"is live is {isLive}");
        }
        query +=" order by a.EventDate DESC";
        _logger.LogInformation($"Query used is {query}");

        using var cmd = new MySqlCommand(query, conn);  
        cmd.Parameters.AddWithValue("@customerId", customerId);
        //only including 6 months worth of events in past.
        if (includePastEvents.HasValue && includePastEvents.Value)
        {
          cmd.Parameters.AddWithValue("@eventDate",DateTime.UtcNow.AddMonths(-6));
        }
        else
        {
          cmd.Parameters.AddWithValue("@eventDate",DateTime.UtcNow);
        }
        
        if (isLive.HasValue)
          cmd.Parameters.AddWithValue("@isLive", isLive.Value);


        List<EventHeader> events = new List<EventHeader>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
          events.Add(
            new EventHeader
            {
              EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
              RefundMode = reader.IsDBNull(reader.GetOrdinal("RefundMode"))?0: (RefundMode)Enum.Parse(typeof(RefundMode), reader.GetString(reader.GetOrdinal("RefundMode"))),
              TicketFeeMode = reader.IsDBNull(reader.GetOrdinal("TicketFeeDisplayMode"))?0: (TicketFeeMode)Enum.Parse(typeof(TicketFeeMode), reader.GetString(reader.GetOrdinal("TicketFeeDisplayMode"))),  

              EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
              EventBannerUrl= reader.IsDBNull(reader.GetOrdinal("EventBannerFileName")) ? string.Empty : 
                                AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString(reader.GetOrdinal("EventBannerFileName"))),
              EventName = reader.GetString(reader.GetOrdinal("EventName")),
              EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))
                               ?StringUtils.CreateUrlSlug(reader.GetString(reader.GetOrdinal("EventName")))
                               :reader.GetString(reader.GetOrdinal("EventUrlName")),
              EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventHeadline")),
              EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
              IsLive = reader.GetBoolean(reader.GetOrdinal("IsLive")),
              Duration = reader.GetInt16(reader.GetOrdinal("Duration")),
              Free = reader.GetBoolean(reader.GetOrdinal("Free")),
              EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
            });
        }
        return events;
      }
     
    }

    public async Task<List<EventHeader>> GetEventListForscanningByCustomerId(int customerId)
    {
      if (customerId <= 0)
        throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        //todo: maybe get all events including past events 
        var query = @"select a.EventId,a.EventName, a.EventUrlName,a.EventHeadline,a.EventDate, a.EventBannerFileName,
                    a.EventOrganizer,  a.EventSummary,a.Free,
                    ifnull(a.EventAddress,'') as EventAddress
                    from events a
                    WHERE a.EventOrganizer= @customerId and a.EventDate>=UTC_DATE() and a.IsLive=1
                    order by a.EventDate ASC";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@customerId", customerId);

        List<EventHeader> events = new List<EventHeader>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
          events.Add(
            new EventHeader
            {
              EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
              EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
              EventBannerUrl= reader.IsDBNull(reader.GetOrdinal("EventBannerFileName")) ? string.Empty : 
                                AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString(reader.GetOrdinal("EventBannerFileName"))),
              EventName = reader.GetString(reader.GetOrdinal("EventName")),
              EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))
                               ?StringUtils.CreateUrlSlug(reader.GetString(reader.GetOrdinal("EventName")))
                               :reader.GetString(reader.GetOrdinal("EventUrlName")),
              EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventHeadline")),
              EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
              Free = reader.GetBoolean(reader.GetOrdinal("Free")),
              EventLocation = reader.IsDBNull(reader.GetOrdinal("EventAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventAddress"))
            });
        }
        return events;
      }
     
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

        var query = @"select a.*, b.OrganizationName, b.OrganizerEventBaseUrl from events a
                      inner join eventorganizer b on b.CustomerId=a.eventorganizer
                      WHERE
                      EventId = @eventId";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return GetEventFromReader(reader);
        }
      }
      throw new Exception($"Unable to locate event with Id {eventId}");
    }

    public async Task<Event> GetEventDetailsByName(string customerUrlName, string eventUrlName)
    {
      if (string.IsNullOrWhiteSpace(eventUrlName) || string.IsNullOrWhiteSpace(customerUrlName))
          throw new ArgumentException("Invalid arguments for function");

      string cacheKey = CacheHelper.GetCacheKey<Event>(string.Format("{0}_{1}",customerUrlName,eventUrlName));
      Event? cachedEvent = await _cache.GetOrSetAsync(cacheKey, () => GetEventDetailsByNameFromDb(customerUrlName, eventUrlName), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
     
      return cachedEvent ?? throw new KeyNotFoundException($"Event with  {cacheKey} not found.") ;
    }

    public async Task<Event> GetEventDetailsByNameFromDb(string customerUrlName, string eventUrlName)
    {
      if (string.IsNullOrWhiteSpace(eventUrlName) || string.IsNullOrWhiteSpace(customerUrlName))
          throw new ArgumentException("Invalid arguments for function");

      using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
      {
        await conn.OpenAsync();

        var query = @"select a.*, b.OrganizationName, b.OrganizerEventBaseUrl 
                    from events a
                    inner join eventorganizer b on b.CustomerId = a.eventOrganizer
                    where a.EventUrlName = @eventUrlName and 
                    b.OrganizerEventBaseUrl = @customerUrlName 
                    and a.IsLive = 1";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@eventUrlName", eventUrlName);
        cmd.Parameters.AddWithValue("@customerUrlName", customerUrlName);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
          return GetEventFromReader(reader);
        }
      }
      _logger.LogError("Unable to locate event by customner {0} and event {1)",customerUrlName,eventUrlName);
      
      throw new Exception($"Unable to locate event with event url name {eventUrlName} and customer url name {customerUrlName}");
    }

    private Event GetEventFromReader(DbDataReader reader)
    {
      return new Event
      {
        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
        IsLive = reader.GetBoolean(reader.GetOrdinal("IsLive")),
        OrganizerUrlName = reader.IsDBNull(reader.GetOrdinal("OrganizerEventBaseUrl")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerEventBaseUrl")),
        EventOrganizer = reader.IsDBNull(reader.GetOrdinal("OrganizationName")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizationName")),
        EventOrganizerId = reader.GetInt32(reader.GetOrdinal("EventOrganizer")),
        LocationId = reader.IsDBNull(reader.GetOrdinal("LocationId")) ? string.Empty : reader.GetString(reader.GetOrdinal("LocationId")),
        EventBannerUrl= reader.IsDBNull(reader.GetOrdinal("EventBannerFileName")) ? string.Empty : 
                            AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString(reader.GetOrdinal("EventBannerFileName"))),
        EventName = reader.GetString(reader.GetOrdinal("EventName")),
        EventUrlName = reader.IsDBNull(reader.GetOrdinal("EventUrlName"))
                            ?StringUtils.CreateUrlSlug(reader.GetString(reader.GetOrdinal("EventName")))
                            :reader.GetString(reader.GetOrdinal("EventUrlName")),
        EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventHeadline")),
        EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
        EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventSummary")),
        Free = reader.GetBoolean(reader.GetOrdinal("Free")),
        RefundMode = reader.IsDBNull(reader.GetOrdinal("RefundMode"))?0: (RefundMode)Enum.Parse(typeof(RefundMode), reader.GetString(reader.GetOrdinal("RefundMode"))),
        TicketFeeMode = reader.IsDBNull(reader.GetOrdinal("TicketFeeDisplayMode"))?0: (TicketFeeMode)Enum.Parse(typeof(TicketFeeMode), reader.GetString(reader.GetOrdinal("TicketFeeDisplayMode"))),  
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

    public async Task<int> CreateEvent(Event evt)
    {
      if (evt == null)
        throw new ArgumentNullException(nameof(evt));

      using var connection = new MySqlConnection(this.ConnectionString);
      await connection.OpenAsync();

      string query = @"INSERT INTO events 
            (EventName,EventUrlName,EventHeadline, EventDescription, EventDate, Duration,
            EventOrganizer, EventAddress,EventAgenda, EventTags,IsLive,Private,
            Latitude,Longitude,StreetAddress,City,State,ZipCode,EventCategory,LocationId,
            CreatedAt) 
            VALUES (@name,@eventUrlName,@headline, @desc, @date, @duration,
             @organizer, @location, @agenda, @tags, @isLive, @isPrivate,
             @lat,@long,@streetAddress, @city, @state, @zipCode, @eventCategory,@locationId, @createdAt)";

      using var cmd = new MySqlCommand(query, connection);
      cmd.Parameters.AddWithValue("@name", evt.EventName);
      cmd.Parameters.AddWithValue("@eventUrlName",evt.EventUrlName);
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

      cmd.Parameters.AddWithValue("@eventCategory",evt.Category);
      cmd.Parameters.AddWithValue("@locationId",evt.LocationId);
      cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

      int rowsAffected = await cmd.ExecuteNonQueryAsync();
      evt.EventId = Convert.ToInt32(cmd.LastInsertedId);
      _cache.AddOrUpdateCache(evt, evt.EventId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));

      return rowsAffected > 0 ? evt.EventId : 0;
    }

    public async Task<bool> DeleteEvent(int eventId)
    {
        if (eventId <= 0)
            throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = "DELETE FROM events WHERE EventId = @eventId";
        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected > 0)
        {
            _cache.RemoveCache<Event>(eventId.ToString());
        }
        return rowsAffected > 0;
    }
    public async Task<bool> UpdateEventSettings(int eventId, EventSettings settings)
    {
        if (eventId <=0)
            throw new ArgumentNullException(nameof(eventId));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = @"UPDATE events SET   
            IsLive = @isLive,
            RefundMode = @refundMode,
            TicketFeeDisplayMode = @ticketFeeDisplayMode
            WHERE EventId = @eventId";

      using var cmd = new MySqlCommand(query, connection);
 
      cmd.Parameters.AddWithValue("@isLive", settings.IsLive);
      cmd.Parameters.AddWithValue("@refundMode", settings.RefundMode.ToString());
      cmd.Parameters.AddWithValue("@ticketFeeDisplayMode", settings.TicketFeeMode.ToString());
      
      cmd.Parameters.AddWithValue("@eventId", eventId);

      int rowsAffected = await cmd.ExecuteNonQueryAsync();
      if (rowsAffected > 0)
      {
        
        string key = CacheHelper.GetCacheKey<Event>(string.Format("{0}_{1}",settings.OrganizerUrlName,settings.EventUrlName));       
        await _cache.RemoveAsyncHelper(key);
        key = CacheHelper.GetCacheKey<Event>(eventId.ToString());
        await _cache.RemoveAsyncHelper(key);
        key = CacheHelper.GetCacheKey<EventHeader>(eventId.ToString()); 
        await _cache.RemoveAsyncHelper(key);
          
      }
      return rowsAffected > 0;

    }

    public async Task<bool> UpdateEventBannerImageUrl(int eventId, string url)
    {
        if (eventId <=0 || string.IsNullOrWhiteSpace(url))
          throw new ArgumentException("Invalid input args for function");
        
        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();
        string query = @"UPDATE events SET 
            EventBannerFileName = @name
            where eventid=@eventId";
        using var cmd = new MySqlCommand(query, connection);
  
        cmd.Parameters.AddWithValue("@name", url);
        cmd.Parameters.AddWithValue("@eventId", eventId);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected > 0)
        { 
          string cacheKey = CacheHelper.GetCacheKey<Event>(eventId.ToString());
          Event? evt = await _cache.GetOnlyAsync<Event>(cacheKey);
          if (evt!=null)
          {
              evt.EventBannerUrl =AmazonS3ContentUploader.ConvertKeyToUrl(url);
              _cache.AddOrUpdateCache<Event>(evt,eventId.ToString(),TimeSpan.FromMinutes(base._cacheDurationInMinutes));
              if (!string.IsNullOrWhiteSpace(evt.EventUrlName) && !string.IsNullOrWhiteSpace(evt.OrganizerUrlName))
                _cache.AddOrUpdateCache<Event>(evt, string.Format("{0}_{1}",evt.OrganizerUrlName,evt.EventUrlName),
                    TimeSpan.FromMinutes(base._cacheDurationInMinutes));
              _cache.AddOrUpdateCache<EventHeader>(evt,eventId.ToString(),TimeSpan.FromMinutes(base._cacheDurationInMinutes));
          }
        }
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateEvent(Event evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        using var connection = new MySqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        string query = @"UPDATE events SET 
            EventName = @name,
            EventUrlName = @eventUrlName,
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
            EventCategory = @eventCategory,
            LocationId=@locationId,
            ModifiedAt = @modifiedAt
            WHERE EventId = @eventId";

      using var cmd = new MySqlCommand(query, connection);
 
      cmd.Parameters.AddWithValue("@name", evt.EventName);
      cmd.Parameters.AddWithValue("@eventUrlName", evt.EventUrlName);
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
      cmd.Parameters.AddWithValue("@eventCategory",evt.Category);
      cmd.Parameters.AddWithValue("@locationId",evt.LocationId);
      cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

    //todo: The remaining fields for event which are not set will get overwritten with blank
    //in the cache. need to fix this.
      int rowsAffected = await cmd.ExecuteNonQueryAsync();
      if (rowsAffected > 0)
      { 
      
          string cacheKey = CacheHelper.GetCacheKey<Event>(evt.EventId.ToString());
          bool result = await _cache.UpdatePartialAsync<Event>(cacheKey,evt);
          _logger.LogInformation($"Result for updating  event cache with key{cacheKey} is {result}");
          //_cache.AddOrUpdateCache(evt, evt.EventId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
          cacheKey = CacheHelper.GetCacheKey<Event>(string.Format("{0}_{1}",evt.OrganizerUrlName,evt.EventUrlName));
          result = await _cache.UpdatePartialAsync<Event>(cacheKey,evt);
          _logger.LogInformation($"Result for updating  event cache with key {cacheKey} is {result}");
         // _cache.AddOrUpdateCache(evt, string.Format("{0}_{1}",evt.OrganizerUrlName,evt.EventUrlName),TimeSpan.FromMinutes(base._cacheDurationInMinutes));
          //_cache.AddOrUpdateCache<EventHeader>(evt, evt.EventId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));
          cacheKey = CacheHelper.GetCacheKey<Event>(evt.EventId.ToString());
          result = await _cache.UpdatePartialAsync<Event>(cacheKey,evt);
          _logger.LogInformation($"Result for updating  event cache with key {cacheKey} is {result}");
         
      }
      return rowsAffected > 0;
    }

  }
}
