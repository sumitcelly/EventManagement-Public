using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace EventManagementDbAccess
{
    public class SalesOrderDbAccess :BaseDbAccess
    {
        public SalesOrderDbAccess(IConfiguration connectionString, ILogger<SalesOrderDbAccess> logger) : base(connectionString, logger)
        {
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

                string query = @"select a.EventId, a.SalesOrderCode,b.EventName,
                                b.EventHeadline,b.EventDate, b.EventOrganizer, b.EventAddress,
                                b.EventSummary,b.Free
                                from SalesOrder a, Events b
                                where a.EventId = b.EventId and
                                a.UserId=@userId and b.eventDate>UTC_DATE()";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", userId);
                List<UserSalesOrders> events = new List<UserSalesOrders>();
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        events.Add(new UserSalesOrders()
                        {
                            SalesOrderCode =  reader.GetString("SalesOrderCode"),
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

        public async Task<bool> UpdateSalesOrderStatusAndStripeSessionId(int orderId, SalesOrderStatus status, string stripeSessionId)
        {
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than zero.", nameof(orderId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE salesorder 
                                 SET SalesOrderStatus = @status, 
                                     StripeSessionId = @stripeSessionId, 
                                     ModifiedAt = @modifiedAt
                                 WHERE OrderId = @orderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", (int)status);
                cmd.Parameters.AddWithValue("@stripeSessionId", stripeSessionId ?? string.Empty);
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
                                Count(*) AS OrderCount
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
                query += @" ORDER BY a.CreatedAt " + ASC + "LIMIT @limit;";

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
                            SalesOrderStatus = int.TryParse(reader.GetString("SalesOrderStatus"), out int statusValue) ? ((SalesOrderStatus)statusValue).ToString() : SalesOrderStatus.InProgress.ToString(),
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