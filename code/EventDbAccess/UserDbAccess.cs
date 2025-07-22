using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace EventManagementDbAccess
{
    public class UserDbAccess :BaseDbAccess
    {

        public UserDbAccess(IConfiguration config, ILogger<UserDbAccess> logger) : base(config, logger)
        {
        }

        public async Task<int> CreateUser(EventUser attendee)
        {
            if (attendee == null)
                throw new ArgumentNullException(nameof(attendee));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO eventuser (
                    FullName, Email, Sms, City, Country, 
                    StreetAddress, ZipCode,  
                    Password, CreatedAt, ModifiedAt
                ) VALUES (
                    @name, @email, @sms, @city, @country,
                    @streetAddress, @zipCode,
                    @password, @createdAt, @modifiedAt
                )";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@name", attendee.Name);
                cmd.Parameters.AddWithValue("@email", attendee.Email);
                cmd.Parameters.AddWithValue("@sms", attendee.Sms);
                cmd.Parameters.AddWithValue("@city", attendee.City);
                cmd.Parameters.AddWithValue("@country", attendee.Country);
                cmd.Parameters.AddWithValue("@streetAddress", attendee.StreetAddress);
                cmd.Parameters.AddWithValue("@zipCode", attendee.ZipCode);

                cmd.Parameters.AddWithValue("@password", attendee.Password);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return cmd.LastInsertedId > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
                    // Get the last inserted ID
                }
                else
                {
                    throw new Exception("Failed to create attendee.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating attendee: {ex.Message}");
                throw;
            }
        }

        public async Task<EventUser> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                    UserId, FullName, Email, Sms, City, 
                    Country, StreetAddress, ZipCode,
                    CreatedAt, ModifiedAt 
                    FROM eventuser 
                    WHERE Email = @email";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventUser
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Sms = reader.IsDBNull(reader.GetOrdinal("Sms")) ? 
                              string.Empty : 
                              reader.GetString(reader.GetOrdinal("Sms")),
                        City = reader.IsDBNull(reader.GetOrdinal("City")) ? 
                               string.Empty : 
                               reader.GetString(reader.GetOrdinal("City")),
                        Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? 
                                 string.Empty : 
                                 reader.GetString(reader.GetOrdinal("Country")),
                        StreetAddress = reader.IsDBNull(reader.GetOrdinal("StreetAddress")) ? 
                                       string.Empty : 
                                       reader.GetString(reader.GetOrdinal("StreetAddress")),
                        ZipCode = reader.IsDBNull(reader.GetOrdinal("ZipCode")) ? 
                                 string.Empty : 
                                 reader.GetString(reader.GetOrdinal("ZipCode")),
                       
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateUser(EventUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventuser SET
                                    FullName = @name,
                                    Email = @email,
                                    Sms = @sms,
                                    City = @city,
                                    Country = @country,
                                    StreetAddress = @streetAddress,
                                    ZipCode = @zipCode,
                                    Password = @password,
                                    ModifiedAt = @modifiedAt
                                 WHERE UserId = @userId";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@sms", user.Sms);
                cmd.Parameters.AddWithValue("@city", user.City);
                cmd.Parameters.AddWithValue("@country", user.Country);
                cmd.Parameters.AddWithValue("@streetAddress", user.StreetAddress);
                cmd.Parameters.AddWithValue("@zipCode", user.ZipCode);
                cmd.Parameters.AddWithValue("@password", user.Password);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@userId", user.UserId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"DELETE FROM eventuser WHERE Email = @email";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@email", email);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user by email: {ex.Message}");
                throw;
            }
        }
    }
}