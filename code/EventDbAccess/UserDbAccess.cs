using EventUtils;
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
                    Password, PasswordSalt,CreatedAt, ModifiedAt
                ) VALUES (
                    @name, @email, @sms, @city, @country,
                    @streetAddress, @zipCode,
                    @password,@passwordSalt, @createdAt, @modifiedAt
                )";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@name", attendee.Name);
                cmd.Parameters.AddWithValue("@email", attendee.Email);
                cmd.Parameters.AddWithValue("@sms", attendee.Sms);
                cmd.Parameters.AddWithValue("@city", attendee.City);
                cmd.Parameters.AddWithValue("@country", attendee.Country);
                cmd.Parameters.AddWithValue("@streetAddress", attendee.StreetAddress);
                cmd.Parameters.AddWithValue("@zipCode", attendee.ZipCode);
                string hashedPassword = PasswordHelper.HashPassword(attendee.Password,  out string passwordSalt);
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                cmd.Parameters.AddWithValue("@passwordSalt", passwordSalt);
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

        public async Task<EventUser> GetUserById(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than zero.", nameof(userId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                        UserId, FullName, Email, Sms, City, 
                        Country, StreetAddress, ZipCode,
                        CreatedAt, ModifiedAt 
                    FROM eventuser 
                    WHERE UserId = @userId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", userId);

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
                Console.WriteLine($"Error retrieving user by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<EventUser> GetUserByEmailAndPassword(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                string query = @"SELECT 
                    UserId, FullName, Email, Sms, City, Password, PasswordSalt,
                    Country, StreetAddress, ZipCode,
                    CreatedAt, ModifiedAt 
                FROM eventuser 
                WHERE Email = @Email";
                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@Email", email);
              
                using var reader = await cmd.ExecuteReaderAsync();
             
                if (await reader.ReadAsync())
                {
                    string salt = reader.IsDBNull(reader.GetOrdinal("PasswordSalt")) ? string.Empty : reader.GetString(reader.GetOrdinal("PasswordSalt"));
                    string hashedPassword = reader.GetString(reader.GetOrdinal("Password"));
                    if (PasswordHelper.VerifyPassword(password, salt, hashedPassword))
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
                        _logger.LogInformation($"User found: {email}");
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid password for user: {email}");
                        return null; // Password does not match
                    }

                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user by email and password: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> ResetPassword(int userId, string newPassword)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException(nameof(newPassword), "New password cannot be null or empty.");

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                string query = @"UPDATE eventuser 
                                SET Password = @password, PasswordSalt = @passwordSalt,  ModifiedAt = @modifiedAt 
                                WHERE UserId = @userId";
                using var cmd = new MySqlCommand(query, connection);
                string hashedPassword = PasswordHelper.HashPassword(newPassword, out string salt);
                cmd.Parameters.AddWithValue("@passwordSalt", salt);
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@userId", userId);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating password: {ex.Message}");
                throw;
            }
        }

        public async Task<EventUser> UpdatePassword(int userId, string oldPassword, string password)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
            if (string.IsNullOrEmpty(oldPassword))
                throw new ArgumentNullException(nameof(oldPassword), "Old password cannot be null or empty.");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "New password cannot be null or empty.");

            try
            {
                var user = await GetUserById(userId);
                if (user == null)
                    throw new Exception("User not found.");
                if (PasswordHelper.VerifyPassword(oldPassword, user.PasswordSalt, user.Password))
                    throw new Exception("Old password is incorrect.");
                user.Password = password;
                
                bool isUpdated = await UpdateUser(user);
                if (!isUpdated)
                    throw new Exception("Failed to update user password.");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating password: {ex.Message}");
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
                                    PasswordSalt = @passwordSalt,
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
                string hashedPassword = PasswordHelper.HashPassword(user.Password, out string passwordSalt);
                cmd.Parameters.AddWithValue("@passwordSalt", passwordSalt);       
                cmd.Parameters.AddWithValue("@password", hashedPassword);
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