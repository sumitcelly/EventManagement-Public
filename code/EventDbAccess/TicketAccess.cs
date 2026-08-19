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


   /// <summary>
   /// There are 2 caches here. One is storing tickets by Salesordercode used in getticketdetails on screen
   /// and also for pdf tickets
   /// The other is {eventid:ticketcode} used for scanning and validating tickets.
   /// </summary>
    public class TicketAccess : BaseDbAccess
    {
        private EventItemTypeDbAccess _eventTypeAccess;
        private readonly IConfiguration _configuration;
        public TicketAccess(IConfiguration config, ILogger<TicketAccess> logger, EventItemTypeDbAccess itemTypeDbAccess, IDistributedCache cache) : base(config, logger, cache)
        {
            _eventTypeAccess = itemTypeDbAccess;
            _configuration = config;
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
            if (code.StartsWith(_configuration["SimulationModeCode"]))
            {
                _logger.LogInformation($"Simulation mode code {code} used for event {eventId}. Ticket validation successful.");
                return "SimulationMode:Successful but cannot grant access to event.";
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

        public async Task<EventSalesItem> GetEventTicketByQRCode(string code, int eventId)
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
                throw new ArgumentNullException(nameof(code));
            }
            if (eventId <= 0)
            {
                throw new ArgumentException(nameof(eventId));
            }

            //Todo: Need a UI model here to return data for attendee plus ticket
            EventSalesItem ticket = new EventSalesItem();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"Select a.FullName, a.Email, a.Sms, 
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketStatus , b.PricePaid
                                from eventmanagement.eventuser a, 
                                eventmanagement.eventsalesitem b where
                                a.UserId=b.UserId and
                                b.EventId= @eventId and 
                                b.TicketCode=@code";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@eventId", eventId);
                    cmd.Parameters.AddWithValue("@code", code);

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

        public async Task<bool> FinalizeTicketsForOrder(int orderID,
                                 MySqlConnection mySqlConnection, 
                                 MySqlTransaction transaction,
                                 bool simulationMode = false)
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
                        updateCmd.Parameters.AddWithValue("@ticketCode",!simulationMode ? PasswordGenerator.GetPassword() : _configuration["SimulationModeCode"]+"_"+PasswordGenerator.GetPassword());
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
        public async Task<int> AddEventTickets(List<EventSalesItem> tickets, bool simulationMode = false)
        {
            int retVal = 0;
            if (tickets == null)
            {
                throw new ArgumentNullException(nameof(tickets));
            }

            if (tickets.Count == 0)
            {
                throw new ArgumentNullException("No tickets provided to add");
            }

            var firstTicket = tickets.First();
            int? eventId = firstTicket?.EventId;
            if (!eventId.HasValue || eventId <= 0)
            {
                throw new InvalidDataException($"Invalid event id received for adding tickets with value: {eventId}");
            }

            int? itemType = firstTicket?.EventItemType?.EventItemTypeId;
            if (!itemType.HasValue || itemType <= 0)
            {
                throw new InvalidDataException($"Invalid event item type id received for adding tickets with value: {eventId}");
            }

            if (tickets.Any(ticket => ticket.EventId != eventId || ticket.EventItemType?.EventItemTypeId != itemType))
            {
                throw new InvalidDataException("All tickets in the batch must belong to the same event and ticket type.");
            }

            if (tickets.Any(ticket => ticket.User?.UserId is null or <= 0))
            {
                throw new InvalidDataException("Each ticket must have a valid user before it can be inserted.");
            }

            using MySqlConnection mySqlConnection = new(this.ConnectionString);
            await mySqlConnection.OpenAsync();

            using var transaction = mySqlConnection.BeginTransaction();
            try
            {
                StringBuilder sb = new();
                if (!simulationMode)
                {
                    if (!await _eventTypeAccess.UpdateTicketSoldCountInCache(eventId.Value, itemType.Value, tickets.Count))
                    {
                        throw new InvalidOperationException($"Unable to update ticket sold count cache for {itemType.Value}");
                    }
                    if (!await _eventTypeAccess.UpdateEventItemTypesSoldCount(eventId.Value, itemType.Value, tickets.Count, mySqlConnection, transaction))
                    {
                        throw new InvalidOperationException($"Unable to update ticket sold count in database for {itemType.Value}");
                    }
                }
                else
                {
                    _logger.LogInformation("Simulation mode enabled. Not updating sold count in cache for eventId: {EventId}, itemType: {ItemType}, ticketsCount: {TicketsCount}", eventId.Value, itemType.Value, tickets.Count);
                }

                //Now generating the tickets.
                sb.Append(@"INSERT INTO eventmanagement.eventsalesitem (EventId,UserId,
                    TicketStatus,TicketCode,SalesOrderId,EventItemTypeId,PricePaid,
                    CreatedAt,ModifiedAt) VALUES ");
                int index = 0;
                var parameters = new List<MySqlParameter>();
                foreach (var ticket in tickets)
                {
                    if (index > 0) sb.Append(",");
                    sb.Append($@"(@EventId{index}, @UserId{index}, @TicketStatus{index},@TicketCode{index},
                                        @SalesOrderId{index}, @EventItemTypeId{index},@PricePaid{index},@CreatedAt{index},@ModifiedAt{index})");

                    parameters.Add(new MySqlParameter($"@EventId{index}", ticket.EventId));
                    parameters.Add(new MySqlParameter($"@UserId{index}", ticket.User!.UserId));
                    parameters.Add(new MySqlParameter($"@TicketStatus{index}", ticket.TicketStatus));
                    parameters.Add(new MySqlParameter($"@TicketCode{index}", ticket.TicketCode));
                    parameters.Add(new MySqlParameter($"@SalesOrderId{index}", ticket.SalesOrderId));
                    parameters.Add(new MySqlParameter($"@EventItemTypeId{index}", ticket.EventItemType!.EventItemTypeId));
                    parameters.Add(new MySqlParameter($"@PricePaid{index}", ticket.PricePaid));
                    parameters.Add(new MySqlParameter($"@CreatedAt{index}", DateTime.UtcNow));
                    parameters.Add(new MySqlParameter($"@ModifiedAt{index}", DateTime.UtcNow));

                    index++;
                }

                using (MySqlCommand cmd = new(sb.ToString(), mySqlConnection, transaction))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    int i = await cmd.ExecuteNonQueryAsync();
                    if (i == tickets.Count)
                    {
                        _logger.LogInformation("Successfully inserted {TicketCount} tickets for sales order {SalesOrderId}.", tickets.Count, tickets.First().SalesOrderId);
                    }
                    else
                    {
                        throw new Exception("Unable to insert ticketrecord");
                    }
                }
                
                await transaction.CommitAsync();
                return retVal;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                retVal = ex.Message.ToLower().Contains("not enough") ? -1 : -2;
                Console.WriteLine(ex.Message + ex.InnerException);
                _logger.LogCritical(ex.Message);

                if (!simulationMode && retVal == -2)
                {
                    bool cacheReverted = await _eventTypeAccess.UpdateTicketSoldCountInCache(eventId.Value, itemType.Value, -tickets.Count);
                    if (!cacheReverted)
                    {
                        _logger.LogWarning("Unable to revert ticket sold count in cache for event {EventId} and item type {ItemType} after a failed insert.", eventId.Value, itemType.Value);
                    }
                }

                return retVal;
            }
        }


        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBySalesOrderCode(string salesOrderCode, int eventId)
        {
            if (string.IsNullOrWhiteSpace(salesOrderCode) || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            string cacheKey = CacheHelper.GetCacheKey<IEnumerable<EventSalesItem>>($"{salesOrderCode}");
            IEnumerable<EventSalesItem>? cachedTicket = await _cache.GetOrSetAsync(cacheKey, () => GetEventTicketBySalesOrderCodeFromDb(salesOrderCode, eventId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return cachedTicket ?? throw new KeyNotFoundException($"Ticket for Sales Order Code {salesOrderCode} and Event ID {eventId} not found.");
        }
        
        

        /// <summary>
        /// gets only completed orders or orders that are payment succeeded. Status of PaymentSucceeded(7) or OrderCompleted(9)
        /// </summary>
        /// <param name="salesOrderQrCode"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBySalesOrderCodeFromDb(string salesOrderQrCode, int eventId)
        {
            if ( string.IsNullOrWhiteSpace(salesOrderQrCode) || eventId <= 0)
                throw new ArgumentException("SalesOrderQrCode and EventId must be greater than zero.");

            List<EventSalesItem> ticketList = new List<EventSalesItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT a.FullName, a.Email, a.Sms, a.UserId, c.Description,
                                c.EventItemTypeId, c.Name as ItemName, b.PricePaid,
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketStatus 
                                FROM eventmanagement.eventuser a
                                INNER JOIN eventmanagement.eventsalesitem b ON a.UserId = b.UserId
                                INNER JOIN eventmanagement.eventitemtype c ON b.EventItemTypeId = c.EventItemTypeId AND b.EventId = c.EventId
                                INNER JOIN eventmanagement.salesorder d ON d.OrderId = b.SalesOrderId
                                WHERE d.SalesOrderCode = @salesOrderCode 
                                AND (d.SalesOrderStatus = 7 or d.SalesOrderStatus=9)
                                AND b.EventId = @eventId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderCode", salesOrderQrCode);
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
                            ticket.QRBase64Image = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(ticket.TicketCode));
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

                    _cache.AddOrUpdateCache(ticketList.AsEnumerable(), salesOrderQrCode, TimeSpan.FromMinutes(base._cacheDurationInMinutes));

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

                    object? result = await cmd.ExecuteScalarAsync();
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