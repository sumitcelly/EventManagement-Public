using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


namespace EventManagementDbAccess
{
    public class LoginCodesDbAccess
    {
        private readonly string _connectionString;
        public LoginCodesDbAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CreateLoginCode(LoginCode code)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand(@"INSERT INTO logincodes (userid, securitycode, expiresat, requestip)
                VALUES (@userid, @securitycode, @expiresat, @requestip); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@userid", code.UserId);
            cmd.Parameters.AddWithValue("@securitycode", code.SecurityCode);
            cmd.Parameters.AddWithValue("@expiresat", code.ExpiresAt);
            cmd.Parameters.AddWithValue("@requestip", code.RequestIp ?? (object)DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return id;
        }


        public async Task<int?> GetUserIdByCode(string code)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT userid FROM logincodes WHERE securitycode = @code", conn);
            cmd.Parameters.AddWithValue("@code", code);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return reader.GetInt16(reader.GetOrdinal("userid"));
            return null;
        }

        public async Task<List<string>> GetLoginCodesByUser(int userId)
        {
            var result = new List<string>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT securitycode FROM logincodes WHERE userid = @userid and expiresat < UTC_TIMESTAMP()", conn);
            cmd.Parameters.AddWithValue("@userid", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(ReadLoginCode(reader));
            return result;
        }

        public async Task<bool> UpdateUsedAt(int userId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new MySqlCommand(@"UPDATE logincodes SET
                usedat = UTC_TIMESTAMP(),
                WHERE userid = @id AND usedat IS NULL", conn);
            
            cmd.Parameters.AddWithValue("@id", userId);
        
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteLoginCodes()
        {
            using var conn = new MySqlConnection(_connectionString);
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