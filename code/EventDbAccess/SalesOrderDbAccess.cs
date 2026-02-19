using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EventManagementDbAccess
{
    public class SalesOrderDbAccess :BaseDbAccess
    {
        private EventItemTypeDbAccess _eventTypeAccess;
        private readonly TicketAccess _ticketAccess;
        public SalesOrderDbAccess(IConfiguration connectionString, 
                                    ILogger<SalesOrderDbAccess> logger, 
                        EventItemTypeDbAccess eventItemTypeDbAccess,
                        TicketAccess ticketAccess,  IDistributedCache cache) : base(connectionString, logger,cache)
        {
            _eventTypeAccess = eventItemTypeDbAccess;
            _ticketAccess = ticketAccess;
        }
        
        public async Task<int> CreateSalesOrder(SalesOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO salesorder 
                    (CustomerId, EventId, UserId, CreatedAt, ModifiedAt,SalesOrderCode, DeliveryType, SalesOrderStatus, StripeSessionId) 
                    VALUES 
                    (@customerId, @eventId, @userId, @createdAt, @modifiedAt,@salesOrderCode,@deliveryType,@salesOrderStatus,@stripeSessionId)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", order.CustomerId);
                cmd.Parameters.AddWithValue("@eventId", order.EventId);
                cmd.Parameters.AddWithValue("@userId", order.UserId);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@salesOrderCode", order.SalesOrderCode); // Ensure SalesOrderCode is not null
                cmd.Parameters.AddWithValue("@deliveryType", order.DeliveryType ?? "Email"); // Default to Email if null
                cmd.Parameters.AddWithValue("@salesOrderStatus", (int)order.SalesOrderStatus);
                cmd.Parameters.AddWithValue("@stripeSessionId", order.StripeSessionId ?? string.Empty); // Default to empty string if null
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new Exception("Failed to create sales order.");
                }
                else
                {
                    // Get the last inserted ID
                    return cmd.LastInsertedId > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifySalesOrderUser(int orderId, int userId)
        {
            if (orderId <= 0 || userId <= 0)
                throw new ArgumentException("OrderId and UserId must be greater than zero.", nameof(orderId));

            try
            {
                string cacheKey = CacheHelper.GetCacheKey<int>(orderId.ToString());
                if (string.IsNullOrEmpty(cacheKey))
                {
                    return false;
                }
                string tempUserId = await _cache.GetOrSetAsync(cacheKey, () => VerifySalesOrderUserFromDb(orderId, userId), TimeSpan.FromMinutes(_cacheDurationInMinutes), _logger) ?? string.Empty;
                if (string.IsNullOrEmpty(tempUserId) || tempUserId == "-1" 
                    || !int.TryParse(tempUserId, out int cachedUserId) || cachedUserId != userId)
                {
                    _logger.LogWarning($"User verification failed for sales order. OrderId: {orderId}, UserId: {userId}");
                    return false;
                }
                else
                {
                    _logger.LogInformation($"User verification successful for sales order. OrderId: {orderId}, UserId: {userId}");
                    return true;
                }   
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying sales order user: {ex.Message}");
                throw;
            }
            
        }
        public async Task<string> VerifySalesOrderUserFromDb(int orderId, int userId)
        {
            if (orderId <= 0 || userId <= 0)
                throw new ArgumentException("OrderId and UserId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM salesorder WHERE OrderId = @orderId AND UserId = @userId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@userId", userId);

                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    _logger.LogInformation($"User verification successful for sales order from DB. OrderId: {orderId}, UserId: {userId}");
                    return userId.ToString();
                }
                else
                {
                    _logger.LogWarning($"User verification failed for sales order. OrderId: {orderId}, UserId: {userId}");
                    return "-1";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying sales order user: {ex.Message}");
                throw;
            }
        }
        public async Task<SalesOrder> GetSalesOrderById(int orderId)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM salesorder WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SalesOrder
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                        SalesOrderCode =reader.IsDBNull(reader.GetOrdinal("SalesOrderCode"))?string.Empty: reader.GetString(reader.GetOrdinal("SalesOrderCode")),
                        DeliveryType = reader.IsDBNull(reader.GetOrdinal("DeliveryType")) ? "Email" : reader.GetString(reader.GetOrdinal("DeliveryType")),
                        SalesOrderStatus = (SalesOrderStatus)reader.GetInt32(reader.GetOrdinal("SalesOrderStatus")),
                        StripeSessionId = reader.IsDBNull(reader.GetOrdinal("StripeSessionId")) ? string.Empty : reader.GetString(reader.GetOrdinal("StripeSessionId")),
                        PaymentIntentId = reader.IsDBNull(reader.GetOrdinal("PaymentIntentId")) ? string.Empty : reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                        RefundId = reader.IsDBNull(reader.GetOrdinal("RefundId")) ? string.Empty : reader.GetString(reader.GetOrdinal("RefundId")),
                        RefundAmount = reader.IsDBNull(reader.GetOrdinal("RefundAmount")) ? 0 : reader.GetInt16(reader.GetOrdinal("RefundAmount")),
                        RefundedAt = reader.IsDBNull(reader.GetOrdinal("RefundedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("RefundedAt")),
                     
                        
                    };
                }
               throw new Exception($"Unable to retrieve order for order id {orderId}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error retrieving sales order: {ex.Message}");
                throw;
            }
        }

      

        public async Task<bool> FinalizeSalesOrder(int salesOrderId, string stripeSessionId, string paymentIntentId)
        {
            if (salesOrderId < 0 && String.IsNullOrEmpty(stripeSessionId))
                throw new ArgumentException("Either salesOrderId or stripeSessionId must be provided.");
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment Intent id cannot be empty when finalizing order.");
            string query = string.Empty;
            if (salesOrderId > 0)
            {
                query = @"UPDATE salesorder 
                            SET SalesOrderStatus = @status, 
                                SalesOrderCode =@orderCode,
                                ModifiedAt = @modifiedAt,
                                PaymentIntentId= @paymentIntentId
                            WHERE OrderId = @orderId";
            }
            else
            {
                query = @"UPDATE salesorder 
                            SET SalesOrderStatus = @status, 
                                SalesOrderCode = @orderCode,
                                ModifiedAt = @modifiedAt,
                                PaymentIntentId= @paymentIntentId
                            WHERE StripeSessionId = @stripeSessionId";
            }       
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            using MySqlTransaction mySqlTransaction =  connection.BeginTransaction();
            try
            { 
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)SalesOrderStatus.PaymentSucceeded);
                cmd.Parameters.AddWithValue("@orderCode",PasswordGenerator.GetPassword()); 
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@paymentIntentId", paymentIntentId);
                if (salesOrderId > 0)
                    cmd.Parameters.AddWithValue("@orderId", salesOrderId);
                else
                    cmd.Parameters.AddWithValue("@stripeSessionId", stripeSessionId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No sales order found to finalize for salesOrderid {salesOrderId} or stripesession {stripeSessionId}");
                    throw new Exception($"No sales order found to finalize for salesOrderid {salesOrderId} or stripesession {stripeSessionId}");
                }
                
                bool result =await _ticketAccess.FinalizeTicketsForOrder(salesOrderId,connection, mySqlTransaction);
                if (!result)
                {
                    throw new Exception($"Failed to finalize tickets for salesOrderid {salesOrderId} or stripesession {stripeSessionId}");
                }
                mySqlTransaction.Commit();
                
            }
            catch (Exception ex)
            {   
                mySqlTransaction.Rollback();
                _logger.LogCritical($"Error finalizing sales order for salesOrderid {salesOrderId} or stripesession {stripeSessionId}: {0}", ex.Message);            
                throw;
            }
            return true;
        }

        /// <summary>
        /// Todo: Cache this method if we find it is being called frequently in a short span of time as part of payment status check in the frontend after checkout, and optimize the db call if needed as well.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<SalesOrder> GetSalesOrderByStripeSessionId(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Invalid session id provided", nameof(sessionId));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM salesorder WHERE StripeSessionId = @sessionId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SalesOrder
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                        SalesOrderCode =reader.IsDBNull(reader.GetOrdinal("SalesOrderCode"))?string.Empty: reader.GetString(reader.GetOrdinal("SalesOrderCode")),
                        DeliveryType = reader.IsDBNull(reader.GetOrdinal("DeliveryType")) ? "Email" : reader.GetString(reader.GetOrdinal("DeliveryType")),
                        SalesOrderStatus = (SalesOrderStatus)reader.GetInt32(reader.GetOrdinal("SalesOrderStatus")),
                        StripeSessionId =  reader.GetString(reader.GetOrdinal("StripeSessionId"))
                    };
                }
                throw new Exception(string.Format("Unable to retrieve by session id {0}",sessionId));
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error retrieving sales order by stripesesion id: {ex.Message}");
                throw;
            }
        }

        public async Task<(string SalesOrderCode, string QrImageBase64)> GetSalesOrderQrImage(int orderId)
        {
            try
            {
                if (orderId <= 0)
                    throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT SalesOrderCode FROM salesorder WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                
            
                using var reader = await cmd.ExecuteReaderAsync();
                string salesOrderCode = string.Empty;
                if (await reader.ReadAsync())
                {
                 
                    salesOrderCode = reader.IsDBNull(reader.GetOrdinal("SalesOrderCode"))?string.Empty:
                                            reader.GetString(reader.GetOrdinal("SalesOrderCode"));
                }
                if (!string.IsNullOrEmpty(salesOrderCode))
                {
                    string qrImageBase64 = System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(salesOrderCode));
                    return (salesOrderCode, qrImageBase64);
                }
                else
                {
                    return (salesOrderCode, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sales order qr image: {ex.Message}");
                throw;
            }
        }

        public async  Task<(bool paid, string SalesOrderCode, string QrImage)>  GetSalesOrderPaymentStatus(int orderId)
        {
            try
            {
                if (orderId <= 0)
                    throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT SalesOrderCode, SalesOrderStatus FROM salesorder WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                
            
                using var reader = await cmd.ExecuteReaderAsync();
                SalesOrderStatus status = SalesOrderStatus.InProgress;
                if (await reader.ReadAsync())
                {
                    int enumStatus = reader.GetInt32(reader.GetOrdinal("SalesOrderStatus"));
                    if (Enum.IsDefined(typeof(SalesOrderStatus), enumStatus))
                        status =  (SalesOrderStatus)enumStatus;
                    _logger.LogInformation($"Sales order status for order id {orderId} is {status}");
                    if (status == SalesOrderStatus.PaymentSucceeded)
                    {
                        string salesOrderCode = reader.IsDBNull(reader.GetOrdinal("SalesOrderCode"))?
                                                string.Empty:
                                                reader.GetString(reader.GetOrdinal("SalesOrderCode"));

                        return (true, salesOrderCode, !string.IsNullOrEmpty(salesOrderCode)?System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(salesOrderCode)):string.Empty);             
                    }
                    else
                    {
                        return (false, string.Empty, string.Empty);
                    }
                }   
                throw new Exception($"Sales order for id {orderId} not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sales order qr image: {ex.Message}");
                throw;
            }
        }




        public async Task<SalesOrder> GetSalesOrderByQrCodeAndEventId(int eventId,string qrCode)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM salesorder WHERE EventId = @eventId and SalesOrderCode=@qrCode";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventId", eventId);
                cmd.Parameters.AddWithValue("@qrCode", qrCode);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SalesOrder
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                        SalesOrderCode = reader.GetString(reader.GetOrdinal("SalesOrderCode")),
                        DeliveryType = reader.IsDBNull(reader.GetOrdinal("DeliveryType")) ? "Email" : reader.GetString(reader.GetOrdinal("DeliveryType")),
                        SalesOrderStatus = (SalesOrderStatus)reader.GetInt32(reader.GetOrdinal("SalesOrderStatus")),
                        StripeSessionId = reader.IsDBNull(reader.GetOrdinal("StripeSessionId")) ? string.Empty : reader.GetString(reader.GetOrdinal("StripeSessionId"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserSalesOrders>> GetUpcomingSalesOrdersForUser(int userId)
        {
            try
            {
                if (userId < 0)
                    throw new ArgumentException("User id is invalid");

                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"select a.EventId, a.SalesOrderCode,a.SalesOrderStatus, a.OrderId,b.EventName,
                                b.EventHeadline,b.EventDate, b.EventOrganizer, b.EventAddress,
                                b.EventSummary,b.Free
                                from SalesOrder a, Events b
                                where a.EventId = b.EventId and
                                a.UserId=@userId and b.eventDate>UTC_DATE() order by a.CreatedAt desc";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", userId);
                List<UserSalesOrders> events = new List<UserSalesOrders>();
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(reader.GetOrdinal("SalesOrderCode")))
                            continue;
                        events.Add(new UserSalesOrders()
                        {
                            SalesOrderStatus = reader.IsDBNull(reader.GetOrdinal("SalesOrderStatus"))?string.Empty:
                                                ((SalesOrderStatus)reader.GetInt16(reader.GetOrdinal("SalesOrderStatus"))).ToString(),
                            SalesOrderCode =  reader.GetString("SalesOrderCode"),
                            SalesOrderId = reader.GetInt16("OrderId"),
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
                return events;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteSalesOrder(int orderId)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM salesorder WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                //should delete all related records in Ticket table if necessary due to foreign key cascade delete
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<DecryptedOrderDetails> GetEmailLinkOrderDetails(string encryptedId)
        {
            if (string.IsNullOrEmpty(encryptedId))
                throw new ArgumentException("EncryptedId cannot be null or empty.", nameof(encryptedId));
            try
            {
                string decryptedString = EncryptionHelper.Decrypt(encryptedId);
                if (!int.TryParse(decryptedString, out int orderId))
                    throw new Exception("Invalid encrypted id format.");
                
                SalesOrder order = await GetSalesOrderById(orderId);
                if (order == null)
                    throw new Exception($"Sales Order could not be retrieved for id {orderId}");
               
                
                return new DecryptedOrderDetails
                {
                    SalesOrderId = orderId,
                    EventId = order.EventId,
                    SalesOrderCode = order.SalesOrderCode ?? "",
                    SalesOrderStatus = ((SalesOrderStatus)order.SalesOrderStatus).ToString(),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting order details: {ex.Message}");
                throw;
            }
        }
       
        public async Task<OrderEmailDetails> GetSampleOrderEmailDetails(int eventId)
        {
            if (eventId <= 0)
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                //make sure to get valid salesorder status
                string query = @"SELECT a.Email, a.FullName, b.OrderId FROM eventuser a, salesorder b 
                                WHERE b.EventId = @eventId and a.UserId=b.UserId   limit 1";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventId", eventId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new OrderEmailDetails
                    {
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        SalesOrderId = reader.GetInt32(reader.GetOrdinal("OrderId"))
                    };
                }
                throw new Exception($"Unable to retrieve order email details for event id {eventId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving order email details: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MarkAllReservedOrdersAsAbandoned(int timeoutMinutes)
        {
            timeoutMinutes = timeoutMinutes <= 0 ? 10 : timeoutMinutes;
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                string query = @"select orderid, stripeSessionId from SalesOrder  
                                WHERE SalesOrderStatus = @reservedStatus 
                               AND DATE_ADD(ReservedAt, INTERVAL @timeoutThreshold MINUTE) < UTC_TIMESTAMP()";
                // string query = @"UPDATE salesorder 
                //                 SET SalesOrderStatus = @abandonedStatus,                         
                //                     ModifiedAt = @modifiedAt
                //                 WHERE SalesOrderStatus = @reservedStatus 
                //                 AND DATE_ADD(ReservedAt, INTERVAL @timeoutThreshold MINUTE) < UTC_TIMESTAMP()";
            
                using var cmd = new MySqlCommand(query, connection);
            
                cmd.Parameters.AddWithValue("@reservedStatus", (int)SalesOrderStatus.Reserved);
                cmd.Parameters.AddWithValue("@timeoutThreshold", timeoutMinutes);
                var reader = await cmd.ExecuteReaderAsync();
                List<string> sessionIds = new List<string>();
                while (await reader.ReadAsync())
                {
                    int orderid = reader.GetInt32(reader.GetOrdinal("orderid"));
                    string sessionid =reader.IsDBNull(reader.GetOrdinal("stripeSessionId"))?
                                      string.Empty:
                                      reader.GetString(reader.GetOrdinal("stripeSessionId"));
                    if (string.IsNullOrWhiteSpace(sessionid))
                    {
                        _logger.LogCritical($"For order id {orderid} session id does not exist even thoug status is Reserved");
                    }
                    else
                    {
                        sessionIds.Add(sessionid);
                    }
                }
                reader.Close();
                sessionIds.ForEach(async session =>
                {
                    _logger.LogInformation($"Returning tickets to pool for session id {session}");
                    bool retVal = await ReturnTicketsToPool(SalesOrderStatus.Abandoned, session);
                    if (!retVal)
                    {
                        _logger.LogCritical($"Unable to return tickets to pool for session id {session}");
                    }
                    else
                    {
                        _logger.LogInformation($"Succefully returned tickets to pool for {session} which was Abandoned");
                    }
                });
               
              
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error marking reserved orders as timed out: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSalesOrderStatusAndStripeSessionId(int orderId, SalesOrderStatus status, string stripeSessionId)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = string.Empty;
                if (status != SalesOrderStatus.Reserved)
                    query = @"UPDATE salesorder 
                                    SET SalesOrderStatus = @status,                         
                                        ModifiedAt = @modifiedAt
                                    WHERE OrderId = @orderId";
                else
                    query = @"UPDATE salesorder 
                                    SET SalesOrderStatus = @status, 
                                        StripeSessionId = @stripeSessionId,
                                        ReservedAt = @reservedAt,
                                        ModifiedAt = @modifiedAt
                                    WHERE OrderId = @orderId";
               

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)status);
                cmd.Parameters.AddWithValue("@stripeSessionId", stripeSessionId ?? string.Empty);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                if (status == SalesOrderStatus.Reserved)
                {
                    cmd.Parameters.AddWithValue("@reservedAt", DateTime.UtcNow);
                }
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSalesOrderStatus(int orderId,SalesOrderStatus status)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE salesorder 
                                    SET SalesOrderStatus = @status,                         
                                        ModifiedAt = @modifiedAt
                                    WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)status);
                //cmd.Parameters.AddWithValue("@stripeSessionId", stripeSessionId ?? string.Empty);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSalesOrderRefundStatus(int orderId,SalesOrderStatus status, string refundId, int refundAmount)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE salesorder 
                                    SET SalesOrderStatus = @status,                         
                                        ModifiedAt = @modifiedAt,
                                        RefundId= @refundId,
                                        RefundAmount = @refundAmount,
                                        RefundedAt = @refundedAt
                                    WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)status);
                cmd.Parameters.AddWithValue("@refundId", refundId );
                cmd.Parameters.AddWithValue("@refundAmount", refundAmount );
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@refundedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
               _logger.LogError($"Error updating sales order for refund: {ex.Message}");
                throw;
            }
        }
     

    /// <summary>
    /// This method updates the sales order status.
    /// This method will also return tickets to pool if order is being cancelled,timedout,refunded or payment failed
    /// </summary>
    /// <param name="status"></param>
    /// <param name="orderId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> ReturnTicketsToPool(SalesOrderStatus status, string stripeSessionId,int userId=0,int orderId=0)
    {
        if (string.IsNullOrEmpty(stripeSessionId) && orderId <= 0)
            throw new ArgumentException("Invalid stripe session id and order id provided");

      if (status == SalesOrderStatus.Reserved || status == SalesOrderStatus.InProgress || status == SalesOrderStatus.PaymentSucceeded || status == SalesOrderStatus.OrderCompleted)
            throw new ArgumentException("Unable to proceed with UpdateSalesOrderStatus dues to satus", nameof(status));
      
        
        SalesOrder? order =null;
        if(!string.IsNullOrWhiteSpace(stripeSessionId))
            order  = await GetSalesOrderByStripeSessionId(stripeSessionId);
        else if (orderId >0)
        {
            order = await GetSalesOrderById(orderId);
        }
        if (order == null)
            throw new Exception($"Unable to find order with session id {stripeSessionId}");

        if (userId > 0 && order.UserId != userId)
            throw new Exception($"User {userId} is not authorized to update order {order.OrderId}");

        using var connection = new MySqlConnection(ConnectionString);   
        await connection.OpenAsync();

        using MySqlTransaction mySqlTransaction =  connection.BeginTransaction();
        try
        {
            string query=@"select eventitemtypeid, count(*) as ticketcount
                            from eventsalesitem
                            where salesorderid=@orderID
                            group by eventitemtypeid";
            using var cmd = new MySqlCommand(query, connection,mySqlTransaction);
            cmd.Parameters.AddWithValue("orderId",order.OrderId);
            using var reader = await cmd.ExecuteReaderAsync();
            Dictionary<int,int> ticketsToReturn = new Dictionary<int,int>();
            while (await reader.ReadAsync())
            {
                int itemTypeId = reader.GetInt32(reader.GetOrdinal("eventitemtypeid"));
                int ticketCount = reader.GetInt32(reader.GetOrdinal("ticketcount"));
                ticketsToReturn[itemTypeId]= ticketCount;
            }
            reader.Close();
            
            //return tickets to pool
            foreach (var item in ticketsToReturn)
            {
                _logger.LogInformation($"Returning {item.Value} tickets to pool for event {order.EventId} and item type {item.Key}");
               await _eventTypeAccess.UpdateEventItemTypesSoldCount(order.EventId, item.Key, -item.Value,connection, mySqlTransaction); 
            }
            
            _logger.LogInformation($"Updating sales order status for order id {order.OrderId} to status {status}");

            query = @"UPDATE salesorder
                    SET SalesOrderStatus = @status,                         
                        ModifiedAt = @modifiedAt
                        WHERE OrderId = @orderId";
            cmd.CommandText = query;
            cmd.Transaction = mySqlTransaction;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@status", (int)status);
            cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@orderId", order.OrderId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected > 0 && 
                await UpdateTicketStatusForSalesOrder(status.ToString(), order.OrderId, connection, mySqlTransaction))
            {
                _logger.LogInformation($"Sales order status updated for order id {order.OrderId} to status {status} with rows affected {rowsAffected}");
                await mySqlTransaction.CommitAsync();
                return rowsAffected > 0;
            }
            else
            {
                //return false;
                throw new Exception($"Unable to update order for id {order.OrderId} to status {status}");
            }
            
        }
        catch (Exception ex)
        {
            await mySqlTransaction.RollbackAsync();
            _logger.LogCritical($"Error updating sales order: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateTicketStatusForSalesOrder(string status, int orderId, MySqlConnection conn, MySqlTransaction trans)
    {
        if (string.IsNullOrEmpty(status) || orderId<=0)
        {
            throw new ArgumentException($"Invalid args for Updating ticket status status: {status} or orderid: {orderId}");
        }
        bool disposeConn =false;
        try
        {
            if (conn == null)
            {
                conn= new MySqlConnection(ConnectionString);   
                await conn.OpenAsync();
                disposeConn = true;
            }
            
            string query = @"UPDATE eventsalesitem
                            SET ticketstatus = @status,                         
                            ModifiedAt = @modifiedAt
                            WHERE SalesOrderId = @orderId";
            using MySqlCommand cmd = new MySqlCommand(query, conn, trans);
            cmd.CommandText = query;
            
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@orderId", orderId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                _logger.LogInformation($"Tickets update to status {status} for order id {orderId}");
                return true;
            }
            throw new Exception($"Unable to updatr tickets for order {orderId}");

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating ticket status {ex}");
            throw;
        }
        finally
        {
            if (disposeConn)
            {conn.Dispose();}
        }
    }
    public async Task<List<SalerOrderReportItems>> SearchByCustomer(int customerId, int eventId,DateOnly startDate, DateOnly endDate,
                                                        string emailAddress, string name, int orderStatus,
                                                        string orderByColumn= "createat", bool isAscending =false,
                                                        string? cursor =null, int? orderIdCursor=null,
                                                        int limit=10)

    {   
        if (customerId <= 0)
        {
            throw new ArgumentException("Invalid customer id provided", nameof(customerId));
        }
      
       // Implement search logic based on the provided parameters.
        // This is a placeholder implementation and should be replaced with actual search logic.
        List<SalerOrderReportItems> salesOrders = new List<SalerOrderReportItems>();
        using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
        {
            await connection.OpenAsync();
            {
                _logger.LogInformation("Connection to database established successfully.");
                string query = @" SELECT a.OrderId, a.SalesOrderCode, a.SalesOrderStatus,a.CreatedAt,
                                b.EventName, c.Email, c.FullName,
                                COALESCE(SUM(e.pricepaid), 0) AS OrderTotal,
                                Count(e.ticketid) AS OrderCount
                                from SalesOrder a
                                JOIN Events b ON a.EventId = b.EventId
                                JOIN EventUser c ON a.UserId = c.UserId
                                LEFT JOIN EventSalesItem e ON e.salesorderid = a.orderid
                                LEFT JOIN eventitemtype d ON d.eventitemtypeid = e.eventitemtypeid
                                WHERE 1=1 and a.customerId = @customerId ";

                DateOnly dtTemp =  DateOnly.FromDateTime(DateTime.Now);
                
                //cannot get future orders
                if (endDate >  dtTemp || endDate == DateOnly.MinValue)
                    endDate= dtTemp;
                if (startDate == DateOnly.MinValue)
                {
                    //default to 30 days before
                    startDate = dtTemp.AddDays(-30);
                }
               
                query += " AND a.CreatedAt between @startDate and @endDate";
                //the comparison means that some results maybe repeated.
                //So if there are multiple events at the same exact date and time, then
                //search results will show an overlap
                DateTime cursorDateTime = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(cursor))
                {              
                    DateTime.TryParse(cursor, out cursorDateTime);
                    if (cursorDateTime != DateTime.MinValue)
                    {
                         query += isAscending ? " AND (a.CreatedAt > @cursor  OR (a.CreatedAt = @cursor AND a.OrderId > @orderIdCursor))"
                                         : " AND (a.CreatedAt < @cursor OR (a.CreatedAt = @cursor AND a.OrderId < @orderIdCursor))";

                        // query += isAscending ? " AND (a.CreatedAt > @cursor)"
                        //                  : " AND (a.CreatedAt < @cursor)";

                    }
                    
                }
                                            
                if (!string.IsNullOrEmpty(emailAddress))
                {
                    query += " AND c.Email like @emailAddress";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    query += " AND c.FullName like @fullname";
                }
                if (orderStatus >0)
                {
                    query += " AND a.SalesOrderStatus = @salesorderstatus";
                }
                if (eventId >0)
                {
                    query += " AND a.EventId = @eventId";
                }
                
                query+=@" GROUP BY
                        a.OrderId,
                        a.SalesOrderCode,
                        a.SalesOrderStatus,
                        a.CreatedAt,
                        b.EventName,
                        c.Email,
                        c.FullName";
                string ASC = isAscending ? " ASC " : " DESC ";
                query += @" ORDER BY a.CreatedAt " + ASC;

                if (limit >0)
                    query += " LIMIT @limit;";
                    
                Console.WriteLine("Final Query: " + query);

                MySqlCommand cmd = new MySqlCommand(query, connection);
                
                cmd.Parameters.AddWithValue("@customerId", customerId);
                cmd.Parameters.AddWithValue("@startDate", startDate.ToDateTime(new TimeOnly(0, 0, 0)));
                cmd.Parameters.AddWithValue("@endDate", endDate.ToDateTime(new TimeOnly(0,0,0)));
                if (!string.IsNullOrEmpty(emailAddress))
                {
                    cmd.Parameters.AddWithValue("@emailAddress", "%"+emailAddress+"%");
                }
                if (!string.IsNullOrEmpty(name))
                {
                    cmd.Parameters.AddWithValue("@fullname","%"+ name+"%");
                }
                if (orderStatus>0)
                {
                    cmd.Parameters.AddWithValue("@salesorderstatus", orderStatus);
                }                
                if (eventId > 0)
                {
                    cmd.Parameters.AddWithValue("@eventId", eventId);
                }

                cmd.Parameters.AddWithValue("@limit", limit);
                if (cursorDateTime != DateTime.MinValue)
                    cmd.Parameters.AddWithValue("@cursor", cursorDateTime);
                if (orderIdCursor >0)
                    cmd.Parameters.AddWithValue("@orderIdCursor", orderIdCursor);
                
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salesOrders.Add(new SalerOrderReportItems
                        {
                            OrderId = reader.GetInt32("OrderId"),
                            SalesOrderStatus = int.TryParse(reader.GetString("SalesOrderStatus"), out int statusValue) ? ((SalesOrderStatus)statusValue).ToString() : SalesOrderStatus.InProgress
                            .ToString(),
                            OrderDate = reader.GetDateTime("CreatedAt"),
                            EventName = reader.GetString("EventName"),
                            FullName = reader.GetString("FullName"),
                            EmailAddress = reader.GetString("Email"),
                            OrderTotal = reader.GetInt32("OrderTotal"),
                            OrderCount = reader.GetInt32("OrderCount")
                        });
                    }
                }
            }
        }
        return salesOrders;
    }
    
    }
}