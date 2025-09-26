using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

using System.Data.Common;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using EventUtils;
namespace EventManagementDbAccess
{

    public class TicketAccess : BaseDbAccess
    {
        public TicketAccess(IConfiguration config, ILogger<TicketAccess> logger, IDistributedCache cache) : base(config, logger, cache)
        {

        }
        public async Task<bool> ValidateTicket(string code, int eventId = 1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }
            bool retVal = false;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$" Update eventmanagement.eventsalesitem set TicketScanned=1  where
                                EventId='{eventId}' and TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    int val = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Records update for {code} is {val}");
                    retVal = val == 1 ? true : false;
                    if (retVal)
                    {
                        _logger.LogInformation($"Ticket with code {code} validated successfully.");
                        _cache.Remove(CacheHelper.GetCacheKey<EventSalesItem>($"{eventId}:{code}"));
                    }
                    else
                    {
                        _logger.LogWarning($"Ticket with code {code} could not be validated.");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return retVal;
        }

        public async Task<EventSalesItem> GetEventTicketByQRCode(string code, int eventId = 1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            string cacheKey = CacheHelper.GetCacheKey<EventSalesItem>($"{eventId}:{code}");
            EventSalesItem? cachedTicket = await _cache.GetOrSetAsync(cacheKey, () => GetEventTicketByQRCodeFromDb(code, eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return cachedTicket ?? throw new KeyNotFoundException($"Ticket with code {code} not found.");
        }

        public async Task<EventSalesItem> GetEventTicketByQRCodeFromDb(string code, int eventId = 1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code");
            }

            //Todo: Need a UI model here to return data for attendee plus ticket
            EventSalesItem ticket = new EventSalesItem();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$"Select a.FullName, a.Email, a.Sms, 
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketScanned 
                                from eventmanagement.EventUser a, 
                                eventmanagement.EventSalesItem b where
                                a.UserId=b.UserId and
                                b.EventId='{eventId}' and b.TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader == null || !reader.HasRows)
                        {
                            throw new KeyNotFoundException($"Ticket with code {code} not found.");
                        }
                        if (reader.RecordsAffected > 1)
                            throw new Exception("More than one record returned for ticket code" + code);

                        while (await reader.ReadAsync())
                        {
                            ticket.User = new EventUser()
                            {
                                Name = reader.GetString(0),
                                Email = reader.GetString(1),
                                Sms =reader.IsDBNull(reader.GetOrdinal("Sms")) ? string.Empty : reader.GetString(reader.GetOrdinal("Sms")),
                            };

                            ticket.CreatedAt = reader.GetDateTime(3);
                            ticket.ModifiedAt = reader.GetDateTime(4);
                            ticket.TicketCode = reader.GetString(5);
                            ticket.TicketScanned = reader.GetInt32(6);

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return ticket;
        }

        public async Task<int> AddEventTicket(EventSalesItem ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            try
            {
                using MySqlConnection mySqlConnection = new MySqlConnection(this.ConnectionString);

                StringBuilder sb = new StringBuilder();
                sb.Append(@"INSERT INTO eventmanagement.eventsalesitem (EventId,UserId,
                        TicketScanned,TicketCode,SalesOrderId,EventItemTypeId,
                        CreatedAt,ModifiedAt) ");
                sb.Append(" VALUES (");

                sb.Append(ticket.EventId);
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.User.UserId);
                sb.Append("'");
                sb.Append(",");
                sb.Append(ticket.TicketScanned);
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.TicketCode);
                sb.Append("'");
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.SalesOrderId);
                sb.Append("'");
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.EventItemType.EventItemTypeId);
                sb.Append("'");
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append("'");
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.ModifiedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append("'");
                sb.Append(")");

                Console.WriteLine(sb.ToString());
                mySqlConnection.Open();
                MySqlCommand cmd = new MySqlCommand(sb.ToString(), mySqlConnection);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i == 1)
                {
                    if (cmd.LastInsertedId > 0)
                    {                     
                        _cache.AddOrUpdateCache(ticket, $"{ticket.EventId}:{ticket.TicketCode}", TimeSpan.FromMinutes(base._cacheDurationInMinutes));
                    }
                    return cmd.LastInsertedId > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
                }
                else
                {
                    throw new Exception("Failed to create ticket.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

          
        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBySalesOrderId(int salesOrderId, int eventId)
        {
            if (salesOrderId <= 0 || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            string cacheKey = CacheHelper.GetCacheKey<IEnumerable<EventSalesItem>>($"{salesOrderId}");
            IEnumerable<EventSalesItem>? cachedTicket = await _cache.GetOrSetAsync(cacheKey, () => GetEventTicketBySalesOrderIdFromDb(salesOrderId, eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return cachedTicket ?? throw new KeyNotFoundException($"Ticket for Sales Order ID {salesOrderId} and Event ID {eventId} not found.");
        }
        
            
        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBySalesOrderIdFromDb(int salesOrderId, int eventId)
        {
            if (salesOrderId <= 0 || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            List<EventSalesItem> ticketList = new List<EventSalesItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT a.FullName, a.Email, a.Sms,a.UserId, c.Description,
                                c.EventItemTypeId,c.Name as ItemName, c.Cost,
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketScanned 
                                from eventmanagement.EventUser a, 
                                eventmanagement.EventSalesItem b,
                                eventmanagement.EventItemType c
                                where a.userid=b.userid
                                AND b.EventItemTypeId = c.EventItemTypeId
                                And b.EventId = c.EventId
                                AND b.SalesOrderId = @salesOrderId 
                                AND b.EventId = @eventId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderId", salesOrderId);
                    cmd.Parameters.AddWithValue("@eventId", eventId);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        _logger.LogInformation($"Records affected: {reader.RecordsAffected}");
                        while (await reader.ReadAsync())
                        {
                            EventSalesItem ticket = new EventSalesItem();
                            ticket.User = new EventUser()
                            {
                                Name = reader.GetString(0),
                                Email = reader.GetString(1),
                                Sms = reader.IsDBNull(reader.GetOrdinal("Sms")) ? string.Empty : reader.GetString(reader.GetOrdinal("Sms")),
                                UserId = reader.GetInt32(3)
                            };

                            ticket.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            ticket.ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));
                            ticket.TicketCode = reader.GetString(reader.GetOrdinal("TicketCode"));
                            ticket.TicketScanned = reader.GetInt32(reader.GetOrdinal("TicketScanned"));
                            ticket.EventItemType = new EventItemType()
                            {
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                                EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                                Name = reader.GetString(reader.GetOrdinal("ItemName"))
                            };
                            ticketList.Add(ticket);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return ticketList;
        }

        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBasicsBySalesOrderQrCodeFromDb(string salesOrderCode, int eventId)
        {
            if (string.IsNullOrWhiteSpace(salesOrderCode) || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            List<EventSalesItem> ticketList = new List<EventSalesItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT a.OrderId, b.TicketCode,b.TicketScanned, c.EventItemTypeId,c.Name 
                                    from SalesOrder a, EventSalesItem b, EventItemType c
                                    where a.OrderId=b.SalesOrderId and
                                    b.EventItemTypeId=c.EventItemTypeId and
                                    a.SalesOrderCode=@salesOrderCode and 
                                    a.eventId=@eventId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderCode", salesOrderCode);
                    cmd.Parameters.AddWithValue("@eventId", eventId);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        _logger.LogInformation($"Records affected: {reader.RecordsAffected}");
                        while (await reader.ReadAsync())
                        {
                            EventSalesItem ticket = new EventSalesItem();
                            ticket.TicketCode = reader.GetString(reader.GetOrdinal("TicketCode"));
                            ticket.TicketScanned = reader.GetInt32(reader.GetOrdinal("TicketScanned"));
                            ticket.EventItemType = new EventItemType()
                            {
                                EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            };
                            ticketList.Add(ticket);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return ticketList;
        }

        /// <summary>
        /// Not used yet
        /// </summary>
        /// <param name="salesOrderId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<int> GetEventTicketCountBySalesOrderId(int salesOrderId)
        {
            if (salesOrderId <= 0)
                throw new ArgumentException("SalesOrderId must be greater than zero.");

            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT COUNT(*) FROM eventmanagement.EventSalesItem WHERE SalesOrderId = @salesOrderId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderId", salesOrderId);

                    object result = await cmd.ExecuteScalarAsync();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<bool> RemoveEventTickets(int salesOrderId, int userId)
        {
            if (salesOrderId <= 0 || userId <= 0)
                throw new ArgumentException("SalesOrderId and UserId must be greater than zero.");

            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"DELETE FROM eventmanagement.EventSalesItem 
                                   WHERE SalesOrderId = @salesOrderId AND UserId = @userId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderId", salesOrderId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation($"Removed {rowsAffected} tickets for Sales Order ID {salesOrderId} and User ID {userId}.");
                        _cache.Remove(CacheHelper.GetCacheKey<IEnumerable<EventSalesItem>>($"{salesOrderId}"));
                    }
                    else
                    {
                        _logger.LogWarning($"No tickets found for Sales Order ID {salesOrderId} and User ID {userId}.");
                    }
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}