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
                    (@customerId, @eventId, @userId, @createdAt, @modifiedAt,@salesOrderCode)";

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
    }
}