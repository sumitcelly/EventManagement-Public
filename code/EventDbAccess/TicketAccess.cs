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
        private EventItemTypeDbAccess _eventTypeAccess;
        public TicketAccess(IConfiguration config, ILogger<TicketAccess> logger, EventItemTypeDbAccess itemTypeDbAccess, IDistributedCache cache) : base(config, logger, cache)
        {
            _eventTypeAccess = itemTypeDbAccess;
        }
        public async Task<string> ValidateTicket(string code, int eventId)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }
            if (eventId < 0)
            {
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));
            }
            bool retVal = false;
            try
            {
                EventSalesItem item = await GetEventTicketByQRCode(code, eventId);
                if (item == null)
                {
                    return "Ticket not found";
                }
                if (item.TicketStatus != TicketStatus.Live.ToString())
                {
                    return $"Unable to proceed since ticket is in {item.TicketStatus}";
                }

                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$"Update eventmanagement.eventsalesitem set TicketStatus='{TicketStatus.Scanned.ToString()}'
                                  where EventId='{eventId}' and TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    int val = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Records update for {code} is {val}");
                    retVal = val == 1 ? true : false;
                    if (retVal)
                    {
                        _logger.LogInformation($"Ticket with code {code} validated successfully.");
                        item.TicketStatus = TicketStatus.Scanned.ToString();
                        _cache.AddOrUpdateCache<EventSalesItem>(item, $"{eventId}:{code}", TimeSpan.FromMinutes(base._cacheDurationInMinutes));
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
            return retVal?"Successful":"Failed";
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

        /// <summary>
        /// Returns list of attendee email and names for a given event
        public async Task<Dictionary<string, string>> GetAttendeeInfoByEventId(int eventId)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));
            }
            var emails = new List<string>();
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"SELECT a.EmailAddress,a.FullName FROM eventuser a,eventsalesitem b WHERE b.EventId = @eventId
                         AND a.UserId=b.UserId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@eventId", eventId);
            using var reader = await cmd.ExecuteReaderAsync();
            var result = new Dictionary<string, string>();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(reader.GetOrdinal("EmailAddress")))
                    result.Add(reader.GetString(reader.GetOrdinal("EmailAddress")), reader.GetString(reader.GetOrdinal("FullName")));
            }
            return result;
        }
        
        public async Task<EventSalesItem> GetEventTicketByQRCodeFromDb(string code, int eventId)
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
                                b.TicketCode, b.TicketStatus , b.PricePaid
                                from eventmanagement.eventuser a, 
                                eventmanagement.eventsalesitem b where
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
                                Sms = reader.IsDBNull(reader.GetOrdinal("Sms")) ? string.Empty : reader.GetString(reader.GetOrdinal("Sms")),
                            };

                            ticket.CreatedAt = reader.GetDateTime(3);
                            ticket.ModifiedAt = reader.GetDateTime(4);
                            ticket.TicketCode = reader.GetString(5);
                            ticket.TicketStatus = reader.GetString(6);
                            ticket.PricePaid = reader.GetDecimal(7);

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

        public async Task<bool> FinalizeTicketsForOrder(int orderID, MySqlConnection mySqlConnection, MySqlTransaction transaction)
        {
            if (orderID <= 0)
            {
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderID));
            }
            try
            {
                string query = @"select ticketid from eventmanagement.eventsalesitem where SalesOrderId=@orderID";
                
                MySqlCommand cmd = new MySqlCommand(query, mySqlConnection, transaction);
                cmd.Parameters.AddWithValue("@orderID", orderID);
                using DbDataReader reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    throw new KeyNotFoundException($"No tickets found for order id {orderID}");
                }
                List<int> ticketIds = new List<int>();
                while (await reader.ReadAsync())
                {
                    ticketIds.Add(reader.GetInt32(0));
                }
                reader.Close();
                if (ticketIds.Count > 0)
                {
                    _logger.LogInformation($"Finalizing {ticketIds.Count} tickets for order id {orderID}");
                    ticketIds.ForEach(ticketId =>
                    {
                        _logger.LogInformation($"Ticket ID to finalize: {ticketId}");
                         query = @"update eventmanagement.eventsalesitem 
                            set ModifiedAt=@modifiedAt,
                            TicketCode = @ticketCode,
                            TicketStatus=@status
                            where TicketId=@ticketId";
                        using MySqlCommand updateCmd = new MySqlCommand(query, mySqlConnection,transaction);
                        updateCmd.Parameters.AddWithValue("@modifiedAt",DateTime.UtcNow);
                        updateCmd.Parameters.AddWithValue("@ticketCode",PasswordGenerator.GetPassword());
                        updateCmd.Parameters.AddWithValue("@ticketId", ticketId);
                        updateCmd.Parameters.AddWithValue("@status", TicketStatus.Live.ToString());
                        int rowsAffected = updateCmd.ExecuteNonQuery();
                        if (rowsAffected != 1)
                        {
                            throw new Exception($"Failed to finalize ticket with ID: {ticketId} for Order ID: {orderID}");
                        }
                        else
                        {
                            _logger.LogInformation($"Succeeded in finalizing ticket with ID: {ticketId} for Order ID: {orderID}");
                        }                        
                    });
                }
                    // Placeholder for any ticket finalization logic
                 
                _logger.LogInformation($"All tickets finalized for order id {orderID}");
              
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to finalize tickets for order id {orderID} due to error {ex.Message}");
                throw;
            }
            return true;
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
                        TicketStatus,TicketCode,SalesOrderId,EventItemTypeId,PricePaid,
                        CreatedAt,ModifiedAt) ");
                sb.Append(" VALUES (");

                sb.Append(ticket.EventId);
                sb.Append(",");
                sb.Append("'");
                sb.Append(ticket.User.UserId);
                sb.Append("'");
                sb.Append(",");
                sb.Append(ticket.TicketStatus);
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
                sb.Append(ticket.EventItemType.Cost);
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

          /// <summary>
        /// Calculates the total of the pricepaid field in eventsalesitem table for a given order ID
        /// </summary>
        /// <param name="orderId">The sales order ID</param>
        /// <returns>The total amount paid for all items in the order</returns>
        public async Task<decimal> GetOrderTotalPrice(int orderId)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than 0.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                //retrieves total only if order status is paymentcompleted or partiallyrefunded
                // string query = @"SELECT COALESCE(SUM(pricepaid), 0) AS TotalPricePaid FROM eventsalesitem es
                //                 INNER JOIN salesorder so ON es.salesorderid = so.orderid
                //                 WHERE so.orderid = @orderId and
                //                 AND (so.salesorderstatus = 7 or so.salesorderstatus=11)";

                string query=@"SELECT COALESCE(SUM(pricepaid), 0) AS TotalPricePaid FROM eventsalesitem where
                             salesorderid=@orderId and ticketstatus=@status";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@status", TicketStatus.Live.ToString());

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader.GetDecimal(0);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating total price paid for order {orderId}: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Here the assumption is all tickets are for same event and same item type
        /// </summary>
        /// <param name="tickets"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<int> AddEventTickets(List<EventSalesItem> tickets, bool insertTicketCode = true)
        {
            int retVal =0;
            if (tickets == null)
            {
                throw new ArgumentNullException(nameof(tickets));
            }
            if (tickets.Count == 0)
            {
                throw new ArgumentNullException("No tickets provided to add");
            }


            int? eventId = tickets.First()?.EventId;
            if (!eventId.HasValue || eventId <= 0)
                throw new InvalidDataException($"Invalid event id received for adding tickets with value: {eventId}");
            int? itemType = tickets.First()?.EventItemType?.EventItemTypeId;
            if (!itemType.HasValue || itemType <= 0)
                throw new InvalidDataException($"Invalid event item type id received for adding tickets with value: {eventId}");

            using MySqlConnection mySqlConnection = new MySqlConnection(this.ConnectionString);
            mySqlConnection.Open();

            using (var transaction = mySqlConnection.BeginTransaction())
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    await _eventTypeAccess.UpdateEventItemTypesSoldCount(eventId.Value, itemType.Value, tickets.Count, mySqlConnection, transaction);
                  
                    sb.Append(@"INSERT INTO eventmanagement.eventsalesitem (EventId,UserId,
                    TicketStatus,TicketCode,SalesOrderId,EventItemTypeId,PricePaid,
                    CreatedAt,ModifiedAt) VALUES ");
                    int index = 0;
                    var parameters = new List<MySqlParameter>();
                    foreach (var ticket in tickets)
                    {
                        if (index > 0) sb.Append(","); // comma between VALUES
                        sb.Append($@"(@EventId{index}, @UserId{index}, @TicketStatus{index},@TicketCode{index},
                                        @SalesOrderId{index}, @EventItemTypeId{index},@PricePaid{index},@CreatedAt{index},@ModifiedAt{index})");

                        parameters.Add(new MySqlParameter($"@EventId{index}", ticket.EventId));
                        parameters.Add(new MySqlParameter($"@UserId{index}", ticket.User.UserId));
                        parameters.Add(new MySqlParameter($"@TicketStatus{index}", ticket.TicketStatus));
                        parameters.Add(new MySqlParameter($"@TicketCode{index}", ticket.TicketCode));
                        parameters.Add(new MySqlParameter($"@SalesOrderId{index}", ticket.SalesOrderId));
                        parameters.Add(new MySqlParameter($"@EventItemTypeId{index}", ticket.EventItemType.EventItemTypeId));
                        parameters.Add(new MySqlParameter($"@PricePaid{index}", ticket.PricePaid));             
                        parameters.Add(new MySqlParameter($"@CreatedAt{index}", DateTime.UtcNow));
                        parameters.Add(new MySqlParameter($"@ModifiedAt{index}", DateTime.UtcNow));

                        index++;
                    }


                    Console.WriteLine(sb.ToString());

                    using (MySqlCommand cmd = new(sb.ToString(), mySqlConnection, transaction))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        int i = cmd.ExecuteNonQuery();
                        if (i == tickets.Count)
                        {
                            _logger.LogInformation($@"Successfully inserted {tickets.Count()} tickets  for sales Order {tickets.First().SalesOrderId} 
                                        Return value for last ticket id is{cmd.LastInsertedId}");

                        }
                        else
                        {
                            throw new Exception("Unable to insert ticketrecord");

                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    retVal = ex.Message.ToLower().Contains("not enough") ? -1 : -2;
                    Console.WriteLine(ex.Message + ex.InnerException);
                    _logger.LogCritical(ex.Message);
                    //set the cache back only if the error was due to a reason different than count being exceeded
                    if (retVal == -2)
                        await _eventTypeAccess.UpdateTicketSoldCountInCache(eventId.Value, itemType.Value, -tickets.Count);
                }
                return retVal;
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
                                c.EventItemTypeId,c.Name as ItemName, b.PricePaid,
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketStatus 
                                from eventmanagement.eventuser a, 
                                eventmanagement.eventsalesitem b,
                                eventmanagement.eventitemtype c
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
                            ticket.TicketCode = !reader.IsDBNull(reader.GetOrdinal("TicketCode"))?
                                                reader.GetString(reader.GetOrdinal("TicketCode")):
                                                 string.Empty;
                            ticket.TicketStatus = !reader.IsDBNull(reader.GetOrdinal("TicketStatus"))?
                                                reader.GetString(reader.GetOrdinal("TicketStatus")):
                                                 string.Empty;
                            ticket.PricePaid = reader.GetDecimal(reader.GetOrdinal("PricePaid"));
                            ticket.EventItemType = new EventItemType()
                            {
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                //Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                                EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                                Name = reader.GetString(reader.GetOrdinal("ItemName"))
                            };
                            ticketList.Add(ticket);
                        }
                    }

                    _cache.AddOrUpdateCache(ticketList.AsEnumerable(), salesOrderId.ToString(), TimeSpan.FromMinutes(base._cacheDurationInMinutes));

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return ticketList;
        }

        public async Task<IEnumerable<EventSalesItem>>  GetEventTicketBasicsBySalesOrderQrCodeFromDb(string salesOrderCode, int eventId, int userId)
        {
            if (string.IsNullOrWhiteSpace(salesOrderCode) || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            List<EventSalesItem> ticketList = new List<EventSalesItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT a.OrderId, b.TicketCode,b.TicketStatus, b.PricePaid, c.EventItemTypeId,c.Name 
                                    from salesorder a, eventsalesitem b, eventitemtype c
                                    where a.OrderId=b.SalesOrderId and
                                    b.EventItemTypeId=c.EventItemTypeId and
                                    a.SalesOrderCode=@salesOrderCode and 
                                    a.eventId=@eventId and a.userId=@userId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderCode", salesOrderCode);
                    cmd.Parameters.AddWithValue("@eventId", eventId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        _logger.LogInformation($"Records affected: {reader.RecordsAffected}");
                        while (await reader.ReadAsync())
                        {
                            EventSalesItem ticket = new EventSalesItem();
                            ticket.TicketCode = !reader.IsDBNull(reader.GetOrdinal("TicketCode"))?
                                                reader.GetString(reader.GetOrdinal("TicketCode")):
                                                 string.Empty;
                            ticket.TicketStatus = !reader.IsDBNull(reader.GetOrdinal("TicketStatus"))?
                                                  reader.GetString(reader.GetOrdinal("TicketStatus")):
                                                  string.Empty;
                            ticket.PricePaid = reader.GetDecimal(reader.GetOrdinal("PricePaid"));
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
                    string sql = @"SELECT COUNT(*) FROM eventmanagement.eventsalesitem WHERE SalesOrderId = @salesOrderId";
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
                    string sql = @"DELETE FROM eventmanagement.eventsalesitem 
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