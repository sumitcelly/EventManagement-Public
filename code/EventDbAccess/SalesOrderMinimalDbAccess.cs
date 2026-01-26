using EventUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EventManagementDbAccess
{
    public class SalesOrderMinimalDbAccess :BaseDbAccess
    {
      
        public SalesOrderMinimalDbAccess(IConfiguration connectionString, 
                                    ILogger<SalesOrderDbAccess> logger
                      ) : base(connectionString, logger)
        {
          
        }

        public async Task<bool> MarkAllReservedOrdersAsTimedOut(int timeoutMinutes)
        {
            timeoutMinutes = timeoutMinutes <= 0 ? 10 : timeoutMinutes;
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                string query = @"UPDATE salesorder 
                                SET SalesOrderStatus = @abandonedStatus,                         
                                    ModifiedAt = @modifiedAt
                                WHERE SalesOrderStatus = @reservedStatus 
                                AND DATE_ADD(ReservedAt, INTERVAL @timeoutThreshold MINUTE) < UTC_TIMESTAMP()";
            
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@abandonedStatus", (int)SalesOrderStatus.Abandoned);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@reservedStatus", (int)SalesOrderStatus.Reserved);
                cmd.Parameters.AddWithValue("@timeoutThreshold", timeoutMinutes);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected >= 0)
                {
                    _logger.LogInformation($"Marked {rowsAffected} reserved orders as timed out.");
                    return true;
                }
              
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error marking reserved orders as timed out: {ex.Message}");
                throw;
            }
        }

    }
}