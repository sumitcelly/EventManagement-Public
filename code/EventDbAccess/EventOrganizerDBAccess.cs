using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

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
                    UserId
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
                        UserId = reader.IsDBNull(14) ? 0 : reader.GetInt32(14)
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
                        UserId
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
                        UserId = reader.IsDBNull(14) ? 0 : reader.GetInt32(14)
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
    }
}