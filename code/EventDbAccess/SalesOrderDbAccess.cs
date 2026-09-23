using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly string _encryptionKey;
        private readonly IConfiguration _configuration;
        public SalesOrderDbAccess(IConfiguration configuration, 
                                    ILogger<SalesOrderDbAccess> logger, 
                                    EventItemTypeDbAccess eventItemTypeDbAccess,
                                    IOptions<EncryptionOptions> encryptionOptions,
                                    TicketAccess ticketAccess,  IDistributedCache cache) : 
                                    base(configuration, logger,cache)
        {
            _eventTypeAccess = eventItemTypeDbAccess;
            _ticketAccess = ticketAccess;
            _encryptionKey = encryptionOptions.Value.SecretKey;
            _configuration = configuration;
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
                    order.OrderId = (int)cmd.LastInsertedId;
                    _cache.AddOrUpdateCache(order, order.OrderId.ToString());
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
            if (orderId<=0)
            {
                 throw new ArgumentNullException(nameof(orderId));
            }

            string cacheKey = CacheHelper.GetCacheKey<SalesOrder>(orderId.ToString());
            SalesOrder? order = await _cache.GetOrSetAsync(cacheKey, () => GetSalesOrderByIdFromDb(orderId), TimeSpan.FromMinutes(10), _logger);
            return order ?? throw new KeyNotFoundException($"Order with id {orderId} not found.");          
        }

        public async Task<SalesOrder> GetSalesOrderByIdFromDb(int orderId)
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
                        SalesOrderTotal = reader.IsDBNull(reader.GetOrdinal("SalesOrderTotal"))?0: reader.GetDecimal(reader.GetOrdinal("SalesOrderTotal"))/100.0m,
                        TotalFees = reader.IsDBNull(reader.GetOrdinal("TotalFees"))?0: reader.GetDecimal(reader.GetOrdinal("TotalFees"))/100.0m,
                        PlatformFees = reader.IsDBNull(reader.GetOrdinal("PlatformFees"))?0: reader.GetDecimal(reader.GetOrdinal("PlatformFees"))/100.0m,
                        SalesTax = reader.IsDBNull(reader.GetOrdinal("SalesTax"))?0: reader.GetDecimal(reader.GetOrdinal("SalesTax"))/100.0m,
                       
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

      

        public async Task<bool> FinalizeSalesOrder(int salesOrderId, string paymentIntentId, decimal salesTotal,
                                                decimal platformFees, decimal totalFeesForTrans,
                                                decimal salesTax, bool simulationMode = false)
        {
            if (salesOrderId < 0)
                throw new ArgumentException("Either salesOrderId  must be provided.");
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment Intent id cannot be empty when finalizing order.");
            string query = string.Empty;
           _logger.LogInformation($"Finalize details OrderId:{salesOrderId}, totalFees:{totalFeesForTrans}, platformFee: {platformFees}");
            query = @"UPDATE salesorder 
                        SET SalesOrderStatus = @status, 
                            SalesOrderCode =@orderCode,
                            ModifiedAt = @modifiedAt,
                            PaymentIntentId= @paymentIntentId,
                            SalesOrderTotal = @salesTotal,
                            PlatformFees = @platformFees,
                            TotalFees = @totalFeesForTrans,
                            SalesTax=@salesTax
                        WHERE OrderId = @orderId";
            
           
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            using MySqlTransaction mySqlTransaction =  connection.BeginTransaction();
            try
            { 
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)SalesOrderStatus.PaymentSucceeded);
                cmd.Parameters.AddWithValue("@orderCode",!simulationMode ? PasswordGenerator.GetPassword() : _configuration["SimulationModeCode"]+"_"+PasswordGenerator.GetPassword()); 
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@paymentIntentId", paymentIntentId);   
                cmd.Parameters.AddWithValue("@salesTotal", salesTotal);
                cmd.Parameters.AddWithValue("@platformFees", platformFees);
                cmd.Parameters.AddWithValue("@totalFeesForTrans",totalFeesForTrans);
                cmd.Parameters.AddWithValue("@salesTax", salesTax);
                cmd.Parameters.AddWithValue("@orderId", salesOrderId);
               
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No sales order found to finalize for salesOrderid {salesOrderId}");
                    throw new Exception($"No sales order found to finalize for salesOrderid {salesOrderId}");
                }
                string key = CacheHelper.GetCacheKey<SalesOrder>(salesOrderId.ToString());
                SalesOrder? orderinCache= await _cache.GetOnlyAsync<SalesOrder>(key);
                if (orderinCache != null)
                {
                    orderinCache.SalesOrderStatus =SalesOrderStatus.PaymentSucceeded;
                    orderinCache.SalesOrderCode = cmd.Parameters["@orderCode"].Value.ToString();
                    orderinCache.PaymentIntentId = paymentIntentId;
                    orderinCache.PlatformFees = platformFees/100.0m;
                    orderinCache.SalesTax = salesTax/100.0m;
                    orderinCache.SalesOrderTotal = salesTotal/100.0m;
                    orderinCache.TotalFees = totalFeesForTrans/100.0m;
                    await _cache.SetOnlyAsync<SalesOrder>(key, orderinCache);
                }
                else
                {
                    _logger.LogWarning($"Order with id {salesOrderId} not found in cache");
                }

                bool result =await _ticketAccess.FinalizeTicketsForOrder(salesOrderId,connection, mySqlTransaction, simulationMode);
                if (!result)
                {
                    throw new Exception($"Failed to finalize tickets for salesOrderid {salesOrderId}");
                }
                mySqlTransaction.Commit();
                
            }
            catch (Exception ex)
            {   
                mySqlTransaction.Rollback();
                _logger.LogCritical($"Error finalizing sales order for salesOrderid {salesOrderId}: {0}", ex.Message);            
                throw;
            }
            return true;
        }

        public async Task<SalesOrderPaymentStatus> GetSalesOrderPostPaymentDetails(int orderId)
        {
            try
            {
                if (orderId <= 0)
                    throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

                SalesOrder order = await GetSalesOrderById(orderId);
                if (order == null)
                {
                    throw new Exception($"Unable to locate order with id {orderId}");
                }

                SalesOrderPaymentStatus paymentStatus =new SalesOrderPaymentStatus()
                {
                    SalesOrderTotal = order.SalesOrderTotal,
                    PlatformFees = order.PlatformFees,
                    TotalFees = order.TotalFees,
                    SalesTax = order.SalesTax,
                    SalesOrderCode = order.SalesOrderCode?? string.Empty,
                    Paid = order.SalesOrderStatus == SalesOrderStatus.PaymentSucceeded,
                    QrImage = !string.IsNullOrEmpty(order.SalesOrderCode) ? System.Convert.ToBase64String(QRCodeUtils.GetQRCodes(order.SalesOrderCode)) : string.Empty
                };
               
                return paymentStatus;

               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving sales order qr image: {ex.Message}");
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

                string query = @"select a.EventId, a.SalesOrderCode,a.SalesOrderStatus, a.OrderId,
                                a.SalesOrderTotal, a.TotalFees, a.PlatformFees,a.SalesTax,b.EventName,
                                b.EventHeadline,b.EventDate, b.EventOrganizer, b.EventAddress,
                                b.EventSummary,b.Free,b.EventUrlName, c.OrganizerEventBaseUrl
                                from salesorder a
                                inner join events b on a.EventId = b.EventId
                                inner join eventorganizer c on c.CustomerId = a.CustomerId and
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
                            SalesTax = reader.IsDBNull(reader.GetOrdinal("SalesTax"))?0: reader.GetDecimal(reader.GetOrdinal("SalesTax"))/100.0m,
                            SalesOrderTotal = reader.IsDBNull(reader.GetOrdinal("SalesOrderTotal"))?0: reader.GetDecimal(reader.GetOrdinal("SalesOrderTotal"))/100.0m,
                            TotalFees = reader.IsDBNull(reader.GetOrdinal("TotalFees"))?0: reader.GetDecimal(reader.GetOrdinal("TotalFees"))/100.0m,
                            PlatformFees = reader.IsDBNull(reader.GetOrdinal("PlatformFees"))?0: reader.GetDecimal(reader.GetOrdinal("PlatformFees"))/100.0m,
                            EventHeadline = reader.IsDBNull(reader.GetOrdinal("EventHeadline")) ? string.Empty : reader.GetString("EventHeadline"),
                            EventDate = reader.GetDateTime("EventDate"),
                            EventOrganizerId = reader.GetInt32("EventOrganizer"),
                            EventSummary = reader.IsDBNull(reader.GetOrdinal("EventSummary")) ? string.Empty : reader.GetString("EventSummary"),
                            Free = reader.GetBoolean("Free"),
                            EventUrlName= reader.GetString("EventUrlName"),
                            OrganizerUrlName= reader.GetString("OrganizerEventBaseUrl"),
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
                if (rowsAffected > 0)
                {
                    await _cache.RemoveAsync(CacheHelper.GetCacheKey<SalesOrder>(orderId.ToString()));
                }
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
                string decryptedString = EncryptionHelper.Decrypt(encryptedId, _encryptionKey);
                if (!int.TryParse(decryptedString, out int orderId))
                    throw new Exception("Invalid encrypted id format.");
                
                SalesOrder order = await GetSalesOrderById(orderId);
                if (order == null)
                    throw new Exception($"Sales Order could not be retrieved for id {orderId}");
               
                _logger.LogInformation($"For order id{decryptedString}, total fees is {order.TotalFees} total is {order.SalesOrderTotal} platform fees{order.PlatformFees}");
                return new DecryptedOrderDetails
                {
                    SalesOrderId = orderId,
                    SalesOrderTotal = order.SalesOrderTotal,
                    TotalFees = order.TotalFees,
                    PlatformFees = order.PlatformFees,
                    SalesTax = order.SalesTax,
                    EventId = order.EventId,
                    UserId = order.UserId,
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
                string query = @"SELECT a.Email, a.FullName, b.OrderId,b.SalesOrderCode,b.SalesOrderTotal FROM eventuser a, salesorder b 
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
                        SalesOrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        SalesOrderTotal = reader.IsDBNull(reader.GetOrdinal("SalesOrderTotal"))?0: reader.GetDecimal(reader.GetOrdinal("SalesOrderTotal"))/100.0m,
                        SalesOrderCode = reader.IsDBNull(reader.GetOrdinal("SalesOrderCode"))?string.Empty: reader.GetString(reader.GetOrdinal("SalesOrderCode"))
                    };
                }
                return new OrderEmailDetails();
                
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
                string query = @"select orderid, stripeSessionId from salesorder  
                                WHERE SalesOrderStatus = @reservedStatus 
                               AND DATE_ADD(ReservedAt, INTERVAL @timeoutThreshold MINUTE) < UTC_TIMESTAMP()";
                
                using var cmd = new MySqlCommand(query, connection);
            
                cmd.Parameters.AddWithValue("@reservedStatus", (int)SalesOrderStatus.Reserved);
                cmd.Parameters.AddWithValue("@timeoutThreshold", timeoutMinutes);
                var reader = await cmd.ExecuteReaderAsync();
                List<int> orderIds = new List<int>();
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
                        orderIds.Add(orderid);
                    }
                }
                reader.Close();
                orderIds.ForEach(async orderId =>
                {
                    _logger.LogInformation($"Returning tickets to pool for order id {orderId}");
                    bool retVal = await ReturnTicketsToPool(SalesOrderStatus.Abandoned,orderId);
                    if (!retVal)
                    {
                        _logger.LogCritical($"Unable to return tickets to pool for session id {orderId}");
                    }
                    else
                    {
                        _logger.LogInformation($"Succefully returned tickets to pool for {orderId} which was Abandoned");
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
                if (rowsAffected > 0)
                {
                    string key = CacheHelper.GetCacheKey<SalesOrder>(orderId.ToString());
                    SalesOrder? orderInCache = await _cache.GetOnlyAsync<SalesOrder>(key);
                    if (orderInCache != null)
                    {
                        orderInCache.SalesOrderStatus = status;
                        orderInCache.StripeSessionId = stripeSessionId ??string.Empty;
                        await _cache.SetOnlyAsync<SalesOrder>(key, orderInCache);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to locate order with id {orderId} in cache.");
                    }
                }
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
                if (rowsAffected > 0)
                {
                    string key = CacheHelper.GetCacheKey<SalesOrder>(orderId.ToString());
                    SalesOrder? orderInCache = await _cache.GetOnlyAsync<SalesOrder>(key);
                    if (orderInCache != null)
                    {
                        orderInCache.SalesOrderStatus = status;
                        await _cache.SetOnlyAsync<SalesOrder>(key, orderInCache);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to locate order with id {orderId} in cache.");
                    }
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating sales order: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSalesOrderRefundStatus(int orderId,SalesOrderStatus status, string refundId, decimal   refundAmount)
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
                
                if (rowsAffected > 0)
                {
                    string key = CacheHelper.GetCacheKey<SalesOrder>(orderId.ToString());
                    SalesOrder? orderInCache = await _cache.GetOnlyAsync<SalesOrder>(key);
                    if (orderInCache != null)
                    {
                        orderInCache.SalesOrderStatus = status;
                        orderInCache.RefundId = refundId;
                        //todo: is this conversion ok?
                        orderInCache.RefundAmount = (int)refundAmount;
                        orderInCache.RefundedAt = DateTime.UtcNow;
                        orderInCache.ModifiedAt = DateTime.UtcNow;
                        await _cache.SetOnlyAsync<SalesOrder>(key, orderInCache);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to locate order with id {orderId} in cache.");
                    }
                }
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
    public async Task<bool> ReturnTicketsToPool(SalesOrderStatus status,int orderId, int userId=0)
    {
        if (orderId <= 0)
            throw new ArgumentException("Invalid stripe session id and order id provided");

        if (status == SalesOrderStatus.Reserved || status == SalesOrderStatus.InProgress || status == SalesOrderStatus.PaymentSucceeded || status == SalesOrderStatus.OrderCompleted)
            throw new ArgumentException("Unable to proceed with UpdateSalesOrderStatus dues to satus", nameof(status));
      
        SalesOrder order = await GetSalesOrderById(orderId);
        
        if (order == null)
            throw new Exception($"Unable to find order with session id {orderId}");

        if (userId > 0 && order.UserId != userId)
            throw new Exception($"User {userId} is not authorized to update order {order.OrderId}");

        if (order.SalesOrderStatus == SalesOrderStatus.Abandoned || 
            order.SalesOrderStatus == SalesOrderStatus.Timedout)
        {
            _logger.LogInformation("Sales order has already been abandanoed or timed out. Aborting return tickets to pool");
            return false;
        }

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
               await _eventTypeAccess.UpdateTicketSoldCountInCache(order.EventId, item.Key, -item.Value);
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
  
            if (rowsAffected > 0)
            {
                //Removing from cache although we can update the cache too as i have done it many other places.
                await _cache.RemoveAsyncHelper(CacheHelper.GetCacheKey<SalesOrder>(order.OrderId.ToString()));
                await UpdateTicketStatusForSalesOrder(status.ToString(), order.OrderId,
                            order.SalesOrderCode ?? string.Empty, connection, mySqlTransaction);
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

    public async Task<bool> UpdateTicketStatusForSalesOrder(string status, int orderId, string salesOrderCode,
                                                            MySqlConnection conn, MySqlTransaction trans)
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
                _logger.LogInformation($"Tickets updated to status {status} for order id {orderId}. Now updating ticket cache with status.");
                //for abandoned or timed out orders, there will be no status code.
                if (!string.IsNullOrWhiteSpace(salesOrderCode))
                {
                    string ticketCachekey = CacheHelper.GetCacheKey<IEnumerable<EventSalesItem>>(salesOrderCode);
                    var ticketList =  await _cache.GetOnlyAsync<IEnumerable<EventSalesItem>>(ticketCachekey);
                    if (ticketList?.Count() > 0)
                    {
                        ticketList?.ToList().ForEach(x => x.TicketStatus= status);
                        await _cache.SetOnlyAsync<IEnumerable<EventSalesItem>>(ticketCachekey,ticketList);
                    }
                    else
                    {
                        _logger.LogInformation($"Tickets not found in ticket cache by sales order code for code {salesOrderCode}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Sales order code not found for order id {orderId}");
                }
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
                                                        int limit=20)

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
            
            _logger.LogInformation("Connection to database established successfully.");
            string query = @" SELECT a.OrderId, a.SalesOrderCode, a.SalesOrderStatus,a.CreatedAt,
                            b.EventName, c.Email, c.FullName,
                            COALESCE(SUM(e.pricepaid), 0) AS OrderTotal,
                            Count(e.ticketid) AS OrderCount
                            from salesorder a
                            JOIN events b ON a.EventId = b.EventId
                            JOIN eventuser c ON a.UserId = c.UserId
                            LEFT JOIN eventsalesitem e ON e.salesorderid = a.orderid
                            LEFT JOIN eventitemtype d ON d.eventitemtypeid = e.eventitemtypeid
                            WHERE 1=1 and a.customerId = @customerId ";

            DateOnly dtTemp =  DateOnly.FromDateTime(DateTime.Now);
            
            //cannot get future orders
            if (endDate >=  dtTemp || endDate == DateOnly.MinValue)
                endDate= dtTemp;
            if (startDate == DateOnly.MinValue)
            {
                //default to 30 days before
                startDate = dtTemp.AddDays(-30);
            }
            DateTime startDateTime =  new(startDate,TimeOnly.MinValue);
            DateTime endDateTime = new(dtTemp,TimeOnly.MaxValue);

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
                
            _logger.LogInformation("Final Query: " + query);
            _logger.LogInformation($"start and end date { startDateTime} { endDateTime}");

            MySqlCommand cmd = new MySqlCommand(query, connection);
            
            cmd.Parameters.AddWithValue("@customerId", customerId);
            cmd.Parameters.AddWithValue("@startDate", startDateTime);
            cmd.Parameters.AddWithValue("@endDate", endDateTime);
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
        return salesOrders;
    }

    /// <summary>
    /// Retrieves sales orders that were created a configurable number of hours ago with SalesTax > 0 and TaxCollected = false.
    /// </summary>
    /// <param name="hoursAgo">The number of hours in the past to look for orders. Default is 24 hours.</param>
    /// <returns>A list of SalesOrder objects matching the criteria.</returns>
    /// <exception cref="ArgumentException">Thrown if hoursAgo is less than or equal to 0.</exception>
    public async Task<List<SalesOrderForTax>> GetSalesOrdersForTaxCollection(int hoursAgo = 24)
    {
        if (hoursAgo <= 0)
            throw new ArgumentException("hoursAgo must be greater than 0.", nameof(hoursAgo));

        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            //all orders which have payment succeeded but tax has not been collected
            string query = @"SELECT a.OrderId,a.SalesOrderCode, a.SalesOrderTotal, a.CustomerId,a.EventId,a.SalesTax,a.CreatedAt,
                            a.PaymentIntentId, b.StripeAccountId, c.EventName
                            FROM salesorder a
                            inner join eventorganizer b on a.CustomerId=b.CustomerId
                            inner join events c on a.EventId=c.EventId
                            WHERE a.CreatedAt >= DATE_SUB(UTC_TIMESTAMP(), INTERVAL @hoursAgo HOUR)
                            AND a.SalesTax > 0
                            AND a.TaxCollected = false
                            AND a.SalesOrderStatus=7
                            ORDER BY CreatedAt DESC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@hoursAgo", hoursAgo);

            var salesOrders = new List<SalesOrderForTax>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                salesOrders.Add(new SalesOrderForTax
                {
                    OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                    EventName = reader.GetString(reader.GetOrdinal("EventName")),
                    SalesOrderCode = reader.GetString(reader.GetOrdinal("SalesOrderCode")),
                    StripeAccountId = reader.GetString(reader.GetOrdinal("StripeAccountId")),
                    OrderTotal = reader.GetDecimal(reader.GetOrdinal("SalesOrderTotal")),
                    PaymentIntentId = reader.IsDBNull(reader.GetOrdinal("PaymentIntentId")) ? string.Empty : reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                    SalesTax = reader.IsDBNull(reader.GetOrdinal("SalesTax")) ? 0 : reader.GetDecimal(reader.GetOrdinal("SalesTax")),
                });
            }

            _logger.LogInformation($"Retrieved {salesOrders.Count} sales orders for tax collection from the past {hoursAgo} hours.");
            return salesOrders;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error retrieving sales orders for tax collection: {ex.Message}");
            throw;
        }
    }
    
    }
}