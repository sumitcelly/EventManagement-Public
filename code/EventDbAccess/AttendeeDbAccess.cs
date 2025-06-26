using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace EventDbAccess
{
    public class AttendeeDbAccess :BaseDbAccess
    {

        public AttendeeDbAccess(IConfiguration config, ILogger<AttendeeDbAccess> logger) : base(config, logger)
        {
        }

        public async Task<int> CreateAttendee(Attendee attendee)
        {
            if (attendee == null)
                throw new ArgumentNullException(nameof(attendee));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO attendee (
                    Name, Email, Sms, City, Country, 
                    StreetAddress, ZipCode, Username, 
                    Password, CreatedAt, ModifiedAt
                ) VALUES (
                    @name, @email, @sms, @city, @country,
                    @streetAddress, @zipCode, @username,
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
                cmd.Parameters.AddWithValue("@username", attendee.Username);
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

        public async Task<Attendee> GetAttendeeByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                    AttendeeId, Name, Email, Sms, City, 
                    Country, StreetAddress, ZipCode, Username,
                    CreatedAt, ModifiedAt 
                    FROM attendee 
                    WHERE Email = @email";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Attendee
                    {
                        AttendeeId = reader.GetInt32(reader.GetOrdinal("AttendeeId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
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
                        Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? 
                                  string.Empty : 
                                  reader.GetString(reader.GetOrdinal("Username")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving attendee: {ex.Message}");
                throw;
            }
        }
    }
}