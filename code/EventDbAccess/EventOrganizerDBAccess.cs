using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using static EventManagementDbAccess.EventOrganizer;

namespace EventManagementDbAccess
{
    public class EventOrganizerDBAccess : BaseDbAccess
    {


        public EventOrganizerDBAccess(IConfiguration connectionString, ILogger<EventOrganizerDBAccess> logger) : base(connectionString, logger) 
        {

         }

        public async Task<EventOrganizer> GetOrganizerByName(string organizerName)
        {
            if (string.IsNullOrEmpty(organizerName))
                throw new ArgumentNullException(nameof(organizerName));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                    CustomerId, 
                    OrganizerName,
                    OrganizerEmail,
                    OrganizerWebsite,
                    OrganizerEventBaseUrl,
                    OrganizerDescription,
                    OrganizerLogo,
                    OrganizerCity,
                    OrganizerCountry,
                    OrganizerPhone,
                    OrganizerStreetAddress,
                    OrganizerZipCode,
                    OrganizerInstagram,
                    OrganizerFacebook,
                    StripeAccountId,
                    StripeConnectAccountStatus
                   
                    
                FROM eventorganizer 
                WHERE OrganizerName = @organizerName";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@organizerName", organizerName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizer
                    {
                        OrganizerId = reader.GetInt32(0),
                        OrganizerName = reader.GetString(1),
                        OrganizerEmail = reader.GetString(2),
                        OrganizerWebsite = reader.IsDBNull(3) ? null : reader.GetString(3),
                        OrganizerEventBaseUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                        OrganizerDescription = reader.IsDBNull(5) ? null : reader.GetString(5),
                        OrganizerLogo = reader.IsDBNull(6) ? Array.Empty<byte>() : (byte[])reader[6],
                        OrganizerCity = reader.IsDBNull(7) ? null : reader.GetString(7),
                        OrganizerCountry = reader.IsDBNull(8) ? null : reader.GetString(8),
                        OrganizerPhone = reader.IsDBNull(9) ? null : reader.GetString(9),
                        OrganizerStreetAddress = reader.IsDBNull(10) ? null : reader.GetString(10),
                        OrgnaizerZipCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                        OrganizerInstagram = reader.IsDBNull(12) ? null : reader.GetString(12),
                        OrganizerFacebook = reader.IsDBNull(13) ? null : reader.GetString(13),
                        StripeAccountId = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                        StripeConnectStatus = reader.IsDBNull(15) 
                            ? default 
                            : Enum.TryParse<StripeAccountStatus>(reader.GetString(15), out var status) ? status : default
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving organizer: {ex.Message}");
                throw;
            }
        }

        public async Task<EventOrganizer> GetOrganizerById(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                        CustomerId, 
                        OrganizerName,
                        OrganizerEmail,
                        OrganizerWebsite,
                        OrganizerEventBaseUrl,
                        OrganizerDescription,
                        OrganizerLogo,
                        OrganizerCity,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook,
                        StripeAccountId,
                        StripeConnectAccountStatus
                        
                    FROM eventorganizer 
                    WHERE CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizer
                    {
                        OrganizerId = reader.GetInt32(0),
                        OrganizerName = reader.GetString(1),
                        OrganizerEmail = reader.GetString(2),
                        OrganizerWebsite = reader.IsDBNull(3) ? null : reader.GetString(3),
                        OrganizerEventBaseUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                        OrganizerDescription = reader.IsDBNull(5) ? null : reader.GetString(5),
                        OrganizerLogo = reader.IsDBNull(6) ? Array.Empty<byte>() : (byte[])reader[6],
                        OrganizerCity = reader.IsDBNull(7) ? null : reader.GetString(7),
                        OrganizerCountry = reader.IsDBNull(8) ? null : reader.GetString(8),
                        OrganizerPhone = reader.IsDBNull(9) ? null : reader.GetString(9),
                        OrganizerStreetAddress = reader.IsDBNull(10) ? null : reader.GetString(10),
                        OrgnaizerZipCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                        OrganizerInstagram = reader.IsDBNull(12) ? null : reader.GetString(12),
                        OrganizerFacebook = reader.IsDBNull(13) ? null : reader.GetString(13),
                        StripeAccountId = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                        StripeConnectStatus = reader.IsDBNull(15) 
                            ? default 
                            : Enum.TryParse<StripeAccountStatus>(reader.GetString(15), out var status) ? status : default
                       
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving organizer by CustomerId: {ex.Message}");
                throw;
            }
        }

        public async Task<int> AddOrganizer(EventOrganizer organizer)
        {
            if (organizer == null)
                throw new ArgumentNullException(nameof(organizer));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO eventorganizer (
                        OrganizerName,
                        OrganizerEmail,
                        OrganizerWebsite,
                        OrganizerEventBaseUrl,
                        OrganizerDescription,
                        OrganizerLogo,
                        OrganizerCity,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook
                    ) VALUES (
                        @name, @email, @website, @eventBaseUrl, @description, @logo, @city, @country,
                        @phone, @streetAddress, @zipCode, @instagram, @facebook
                    )";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", organizer.OrganizerName);
                cmd.Parameters.AddWithValue("@email", organizer.OrganizerEmail);
                cmd.Parameters.AddWithValue("@website", organizer.OrganizerWebsite ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@eventBaseUrl", organizer.OrganizerEventBaseUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", organizer.OrganizerDescription ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@logo", organizer.OrganizerLogo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", organizer.OrganizerCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@country", organizer.OrganizerCountry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", organizer.OrganizerPhone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@streetAddress", organizer.OrganizerStreetAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zipCode", organizer.OrgnaizerZipCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@instagram", organizer.OrganizerInstagram ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@facebook", organizer.OrganizerFacebook ?? (object)DBNull.Value);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding organizer: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteOrganizer(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM eventorganizer WHERE CustomerId = @customerId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting organizer: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateOrganizer(EventOrganizer organizer)
        {
            if (organizer == null)
                throw new ArgumentNullException(nameof(organizer));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventorganizer SET
                        OrganizerName = @name,
                        OrganizerEmail = @email,
                        OrganizerWebsite = @website,
                        OrganizerEventBaseUrl = @eventBaseUrl,
                        OrganizerDescription = @description,
                        OrganizerLogo = @logo,
                        OrganizerCity = @city,
                        OrganizerCountry = @country,
                        OrganizerPhone = @phone,
                        OrganizerStreetAddress = @streetAddress,
                        OrganizerZipCode = @zipCode,
                        OrganizerInstagram = @instagram,
                        OrganizerFacebook = @facebook,
                        StripeAccountId = @StripeAccountId,
                        StripeConnectAccountStatus = @stripeConnectStatus
                    WHERE CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", organizer.OrganizerName);
                cmd.Parameters.AddWithValue("@email", organizer.OrganizerEmail);
                cmd.Parameters.AddWithValue("@website", organizer.OrganizerWebsite ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@eventBaseUrl", organizer.OrganizerEventBaseUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", organizer.OrganizerDescription ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@logo", organizer.OrganizerLogo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", organizer.OrganizerCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@country", organizer.OrganizerCountry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", organizer.OrganizerPhone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@streetAddress", organizer.OrganizerStreetAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zipCode", organizer.OrgnaizerZipCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@instagram", organizer.OrganizerInstagram ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@facebook", organizer.OrganizerFacebook ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@customerId", organizer.OrganizerId);
                cmd.Parameters.AddWithValue("@StripeAccountId", organizer.StripeAccountId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@stripeConnectAccountStatus", organizer.StripeConnectStatus.ToString() ?? (object)DBNull.Value);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating organizer: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateStripeAccountInfo(int organizerId, string stripeAccountId, StripeAccountStatus stripeAccountStatus)
        {
            if (organizerId <= 0)
                throw new ArgumentException("OrganizerId must be greater than zero.", nameof(organizerId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventorganizer 
                                 SET StripeAccountId = @StripeAccountId, 
                                     StripeConnectAccountStatus = @StripeAccountStatus 
                                 WHERE CustomerId = @organizerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StripeAccountId", stripeAccountId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StripeAccountStatus", stripeAccountStatus.ToString() ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@organizerId", organizerId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Stripe account info: {ex.Message}");
                throw;
            }
        }
    }
}