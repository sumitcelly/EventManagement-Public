using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;


namespace EventManagementDbAccess
{
    public class LoginCodesDbAccess: BaseDbAccess
    {
  
        public LoginCodesDbAccess(IConfiguration connectionString, ILogger<EventOrganizerDBAccess> logger) : base(connectionString, logger) 
        {

        }

        public async Task<int> CreateLoginCode(LoginCode code)
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand(@"INSERT INTO logincodes (emailaddress, securitycode, expiresat, requestip,createdat)
                VALUES (@emailaddress, @securitycode, @expiresat, @requestip,utc_timestamp()); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@emailaddress", code.EmailAddress);
            cmd.Parameters.AddWithValue("@securitycode", code.SecurityCode);
            cmd.Parameters.AddWithValue("@expiresat", code.ExpiresAt);
          
            cmd.Parameters.AddWithValue("@requestip", code.RequestIp ?? (object)DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return id;
        }


        public async Task<string?> GetEmailAddressByCode(string code)
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT emailaddress FROM logincodes WHERE securitycode = @code  and expiresat > UTC_TIMESTAMP() and usedat is null", conn);
            cmd.Parameters.AddWithValue("@code", code);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return reader.GetString(reader.GetOrdinal("emailaddress"));
            return null;
        }

        public async Task<List<string>> GetLoginCodesByUser(string emailAddress)
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT securitycode FROM logincodes WHERE emailaddress = @emailaddress and expiresat > UTC_TIMESTAMP() and usedat is null", conn);
            cmd.Parameters.AddWithValue("@emailAddress", emailAddress);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(ReadLoginCode(reader));
            return result;
        }

        public async Task<bool> UpdateUsedAt(string code)
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand(@"UPDATE logincodes SET
                usedat = UTC_TIMESTAMP()
                WHERE securitycode = @code AND usedat IS NULL", conn);
            
            cmd.Parameters.AddWithValue("@code", code);
        
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteLoginCodes()
        {
            using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("DELETE FROM logincodes WHERE expiresat < UTC_TIMESTAMP() ", conn);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private string ReadLoginCode(System.Data.Common.DbDataReader reader)
        {
            return reader.GetString(reader.GetOrdinal("securitycode"));
        
        }
    }
}