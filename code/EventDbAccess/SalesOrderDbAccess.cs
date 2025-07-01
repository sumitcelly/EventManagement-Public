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
                    (CustomerId, EventId, AttendeeId, CreatedAt, ModifiedAt,SalesOrderCode) 
                    VALUES 
                    (@customerId, @eventId, @attendeeId, @createdAt, @modifiedAt,@salesOrderCode)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", order.CustomerId);
                cmd.Parameters.AddWithValue("@eventId", order.EventId);
                cmd.Parameters.AddWithValue("@attendeeId", order.AttendeeId);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@salesOrderCode", order.SalesOrderCode); // Ensure SalesOrderCode is not null

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new Exception("Failed to create sales order.");
                }
                else
                {
                    // Get the last inserted ID
                    return cmd.LastInsertedId >0 ? Convert.ToInt32(cmd.LastInsertedId) :0;
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
                        AttendeeId = reader.GetInt32(reader.GetOrdinal("AttendeeId")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
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
    }
}