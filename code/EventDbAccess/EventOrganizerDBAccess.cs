using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Stripe;
using System;
using System.Threading.Tasks;
using static EventManagementDbAccess.EventOrganizer;

namespace EventManagementDbAccess
{
    public class EventOrganizerDBAccess : BaseDbAccess
    {

        private static readonly string _allOrgsKey="AllOrgs";

        public EventOrganizerDBAccess(IConfiguration connectionString, ILogger<EventOrganizerDBAccess> logger, 
        IDistributedCache cache) : base(connectionString, logger,cache) 
        {

        }

        public async Task<List<string>> GetAllOrgNames()
        {
            string key =  CacheHelper.GetCacheKey<List<string>>(_allOrgsKey);
            return await _cache.GetOrSetAsync<List<string>>(key,()=>GetAllOrgNamesFromDb(), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger) ?? new List<string>();
        }
    
        public async Task<List<string>> GetAllOrgNamesFromDb()
        {

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT OrganizationName FROM eventorganizer";
                using var cmd = new MySqlCommand(query, connection);
                
                var result = await cmd.ExecuteReaderAsync();
                List<string> resultList = new List<string>();
                while (result.Read())
                {
                    resultList.Add(result.GetString(0));
                }
                if (resultList.Count > 0)
                {          
                     _cache.AddOrUpdateCache<List<string>>(resultList,_allOrgsKey);
                }
                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all orgs: {ex.Message}");
                throw;
            }
            
        }

       public async Task<int> GetOrganizerIdByStripeAccountId(string stripeAccountId)
        {
            if (string.IsNullOrWhiteSpace(stripeAccountId))
                throw new ArgumentException("StripeAccountId cannot be null or empty.", nameof(stripeAccountId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT CustomerId FROM eventorganizer WHERE StripeAccountId = @stripeAccountId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@stripeAccountId", stripeAccountId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving organizer ID by Stripe account ID: {ex.Message}");
                throw;
            }
        }

        public async Task<DateTime> GetOrganizerCreatedDate(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                string query = @"SELECT CreatedAt FROM eventorganizer 
                         WHERE CustomerId = @customerId";
                 using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                   return reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                }
                else
                {
                    return DateTime.MinValue;
                }
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving organizer by CustomerId: {ex.Message}");
                throw;
            }

        }

        public async Task<EventOrganizer> GetOrganizerById(int customerId)
        {
            if (customerId <=0)
            {
                throw new ArgumentNullException(nameof(customerId));
            }

            string cacheKey = CacheHelper.GetCacheKey<EventOrganizer>(customerId.ToString());
            EventOrganizer? organizer = await _cache.GetOrSetAsync(cacheKey, () => GetOrganizerByIdFromDb(customerId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return organizer ?? throw new KeyNotFoundException($"organizer  with id {customerId} not found.");
        }
        public async Task<EventOrganizer> GetOrganizerByIdFromDb(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT 
                        CustomerId, 
                        OrganizationName,
                        OrganizerEmail,
                        OrganizerWebsite,
                        OrganizerEventBaseUrl,
                        OrganizerDescription,
                        OrganizerAboutMe,
                        OrganizerImageUrl,
                        OrganizerCity,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook,
                        OrganizerX,
                        StripeAccountId,
                        StripeConnectStatus                      
                    FROM eventorganizer 
                    WHERE CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizer
                    {
                        OrganizerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        OrganizationName = reader.GetString(reader.GetOrdinal("OrganizationName")),
                        OrganizerEmail = reader.GetString(reader.GetOrdinal("OrganizerEmail")),
                        OrganizerAboutMe = reader.GetString(reader.GetOrdinal("OrganizerAboutMe")),
                        OrganizerWebsite = reader.IsDBNull(reader.GetOrdinal("OrganizerWebsite")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerWebsite")),
                        OrganizerEventBaseUrl = reader.GetString(reader.GetOrdinal("OrganizerEventBaseUrl")),
                        OrganizerDescription = reader.IsDBNull(reader.GetOrdinal("OrganizerDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerDescription")),
                        OrganizerImageUrl = reader.IsDBNull(reader.GetOrdinal("OrganizerImageUrl")) ? string.Empty : 
                                    AmazonS3ContentUploader.ConvertKeyToUrl(reader.GetString(reader.GetOrdinal("OrganizerImageUrl"))),
                        OrganizerCity = reader.IsDBNull(reader.GetOrdinal("OrganizerCity")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerCity")),
                        OrganizerCountry = reader.GetString(reader.GetOrdinal("OrganizerCountry")),
                        OrganizerPhone = reader.IsDBNull(reader.GetOrdinal("OrganizerPhone")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerPhone")),
                        OrganizerStreetAddress = reader.IsDBNull(reader.GetOrdinal("OrganizerStreetAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerStreetAddress")),
                        OrgnaizerZipCode = reader.IsDBNull(reader.GetOrdinal("OrganizerZipCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerZipCode")),
                        OrganizerInstagram = reader.IsDBNull(reader.GetOrdinal("OrganizerInstagram")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerInstagram")),
                        OrganizerFacebook = reader.IsDBNull(reader.GetOrdinal("OrganizerFacebook")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerFacebook")),
                        OrganizerX = reader.IsDBNull(reader.GetOrdinal("OrganizerX")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerX")),
                        StripeAccountId = reader.IsDBNull(reader.GetOrdinal("StripeAccountId")) ? string.Empty : reader.GetString(reader.GetOrdinal("StripeAccountId")),
                        StripeConnectStatus = reader.IsDBNull(reader.GetOrdinal("StripeConnectStatus"))
                            ? string.Empty: reader.GetString(reader.GetOrdinal("StripeConnectStatus"))
                            
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
        public async Task<EventOrganizer> GetOrganizerByEventBaseUrl(string eventBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(eventBaseUrl))
                throw new ArgumentException("Organizer url is invalid.", nameof(eventBaseUrl));

            string cacheKey = CacheHelper.GetCacheKey<EventOrganizer>(eventBaseUrl);
            EventOrganizer? organizer = await _cache.GetOrSetAsync(cacheKey, () => GetOrganizerByEventBaseUrlFromDb(eventBaseUrl), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return organizer ?? throw new KeyNotFoundException($"organizer  with url {eventBaseUrl} not found.");
        }

        public async Task<EventOrganizer> GetOrganizerByEventBaseUrlFromDb(string eventBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(eventBaseUrl))
                throw new ArgumentException("Organizer url is invalid.", nameof(eventBaseUrl));

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
                        OrganizerAboutMe,
                        OrganizerImageUrl,
                        OrganizerCity,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook,
                        OrganizerX,
                        StripeAccountId,
                        StripeConnectStatus                      
                    FROM eventorganizer 
                    WHERE OrganizerEventBaseUrl = @eventBaseUrl";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@eventBaseUrl", eventBaseUrl);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizer
                    {
                        OrganizerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        OrganizationName = reader.GetString(reader.GetOrdinal("OrganizationName")),
                        OrganizerEmail = reader.GetString(reader.GetOrdinal("OrganizerEmail")),
                        OrganizerAboutMe = reader.GetString(reader.GetOrdinal("OrganizerAboutMe")),
                        OrganizerWebsite = reader.IsDBNull(reader.GetOrdinal("OrganizerWebsite")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerWebsite")),
                        OrganizerEventBaseUrl = reader.GetString(reader.GetOrdinal("OrganizerEventBaseUrl")),
                        OrganizerDescription = reader.IsDBNull(reader.GetOrdinal("OrganizerDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerDescription")),
                        OrganizerImageUrl = reader.IsDBNull(reader.GetOrdinal("OrganizerImageUrl")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerImageUrl")),
                        OrganizerCity = reader.IsDBNull(reader.GetOrdinal("OrganizerCity")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerCity")),
                        OrganizerCountry = reader.GetString(reader.GetOrdinal("OrganizerCountry")),
                        OrganizerPhone = reader.IsDBNull(reader.GetOrdinal("OrganizerPhone")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerPhone")),
                        OrganizerStreetAddress = reader.IsDBNull(reader.GetOrdinal("OrganizerStreetAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerStreetAddress")),
                        OrgnaizerZipCode = reader.IsDBNull(reader.GetOrdinal("OrganizerZipCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerZipCode")),
                        OrganizerInstagram = reader.IsDBNull(reader.GetOrdinal("OrganizerInstagram")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerInstagram")),
                        OrganizerFacebook = reader.IsDBNull(reader.GetOrdinal("OrganizerFacebook")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrganizerFacebook")),
                        OrganizerX = reader.IsDBNull(reader.GetOrdinal("OrganizerX")) ? string.Empty :  reader.GetString(reader.GetOrdinal("OrganizerX")),
                        StripeAccountId = reader.IsDBNull(reader.GetOrdinal("StripeAccountId")) ? string.Empty : reader.GetString(reader.GetOrdinal("StripeAccountId")),
                        StripeConnectStatus = reader.IsDBNull(reader.GetOrdinal("StripeConnectStatus"))
                            ? string.Empty:
                            reader.GetString(reader.GetOrdinal("StripeConnectStatus"))
                          
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

        public async Task<bool> UpdateOrganizerImageUrl(int organizerId, string url)
        {
            if (organizerId <=0 || string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Invalid input args for function");
            
            using var connection = new MySqlConnection(this.ConnectionString);
            await connection.OpenAsync();
            string query = @"UPDATE eventorganizer SET 
                OrganizerImageUrl = @url
                where customerid=@organizerId";
            using var cmd = new MySqlCommand(query, connection);
    
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@organizerId", organizerId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            { 
                EventOrganizer? evtOrg = await _cache.GetOnlyAsync<EventOrganizer>(organizerId.ToString());
                if (evtOrg!=null)
                {
                    evtOrg.OrganizerImageUrl =AmazonS3ContentUploader.ConvertKeyToUrl(url);
                    await _cache.SetOnlyAsync<EventOrganizer>(organizerId.ToString(),evtOrg);
                }
            }
            return rowsAffected > 0;
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
                        OrganizationName,
                        OrganizerEmail,
                        OrganizerWebsite,
                        OrganizerEventBaseUrl,
                        OrganizerDescription,
                        OrganizerImageUrl,
                        OrganizerAboutMe,
                        OrganizerCity,
                        OrganizerState,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerFullAddress,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook,
                        OrganizerX
                    ) VALUES (
                        @name, @email, @website, @eventBaseUrl, @description, @imageUrl,@aboutMe, @city, @state,@country,
                        @phone,@fullAddress, @streetAddress, @zipCode, @instagram, @facebook,@X
                    )";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", organizer.OrganizationName);
                cmd.Parameters.AddWithValue("@email", organizer.OrganizerEmail);
                cmd.Parameters.AddWithValue("@website", organizer.OrganizerWebsite ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@eventBaseUrl", organizer.OrganizerEventBaseUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", organizer.OrganizerDescription ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@imageUrl", organizer.OrganizerImageUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@aboutMe", organizer.OrganizerAboutMe ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", organizer.OrganizerCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@state", organizer.OrganizerState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@country", organizer.OrganizerCountry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", organizer.OrganizerPhone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@streetAddress", organizer.OrganizerStreetAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fullAddress", organizer.OrganizationFullAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zipCode", organizer.OrgnaizerZipCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@instagram", organizer.OrganizerInstagram ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@facebook", organizer.OrganizerFacebook ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@X", organizer.OrganizerX ?? (object)DBNull.Value);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected >0)
                {
                    _cache.AddOrUpdateCache<EventOrganizer>(organizer,cmd.LastInsertedId.ToString());
                }
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
                        OrganizationName = @name,
                        OrganizerEmail = @email,
                        OrganizerWebsite = @website,
                        OrganizerEventBaseUrl = @eventBaseUrl,
                        OrganizerDescription = @description,
                        OrganizerAboutMe = @aboutMe,
                        OrganizerCity = @city,
                        OrganizerCountry = @country,
                        OrganizerPhone = @phone,
                        OrganizerStreetAddress = @streetAddress,
                        OrganizerZipCode = @zipCode,
                        OrganizerFullAddress = @fullAddress,
                        OrganizerInstagram = @instagram,
                        OrganizerFacebook = @facebook,
                        OrganizerX= @organizerX                     
                    WHERE CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", organizer.OrganizationName);
                cmd.Parameters.AddWithValue("@email", organizer.OrganizerEmail);
                cmd.Parameters.AddWithValue("@website", organizer.OrganizerWebsite ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@eventBaseUrl", organizer.OrganizerEventBaseUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", organizer.OrganizerDescription ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", organizer.OrganizerCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@country", organizer.OrganizerCountry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", organizer.OrganizerPhone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@streetAddress", organizer.OrganizerStreetAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zipCode", organizer.OrgnaizerZipCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@instagram", organizer.OrganizerInstagram ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@facebook", (object)organizer.OrganizerFacebook??DBNull.Value);
                cmd.Parameters.AddWithValue("@customerId", organizer.OrganizerId);
                cmd.Parameters.AddWithValue("@organizerX", organizer.OrganizerX);
                cmd.Parameters.AddWithValue("@aboutMe", organizer.OrganizerAboutMe);
                cmd.Parameters.AddWithValue("@fullAddress", organizer.OrganizationFullAddress);
                //cmd.Parameters.AddWithValue("@StripeAccountId", organizer.StripeAccountId ?? (object)DBNull.Value);
                //cmd.Parameters.AddWithValue("@stripeConnectStatus", organizer.StripeConnectStatus.ToString() ?? (object)DBNull.Value);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    _cache.AddOrUpdateCache<EventOrganizer>(organizer, organizer.OrganizerId.ToString());
                }
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
                                     StripeConnectStatus = @StripeAccountStatus 
                                 WHERE CustomerId = @organizerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StripeAccountId", stripeAccountId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StripeAccountStatus", stripeAccountStatus.ToString() ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@organizerId", organizerId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                { 
                    EventOrganizer? evtOrg = await _cache.GetOnlyAsync<EventOrganizer>(organizerId.ToString());
                    if (evtOrg!=null)
                    {
                        evtOrg.StripeAccountId = stripeAccountId ?? string.Empty;
                        evtOrg.StripeConnectStatus = stripeAccountStatus.ToString();
                        await _cache.SetOnlyAsync<EventOrganizer>(organizerId.ToString(),evtOrg);
                    }
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Stripe account info: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateStripeStatus(int organizerId, string stripeAccountId, StripeAccountStatus stripeAccountStatus)
        {
            if (organizerId <= 0)
                throw new ArgumentException("OrganizerId must be greater than zero.", nameof(organizerId));
            
             if (string.IsNullOrWhiteSpace(stripeAccountId))
                throw new ArgumentException("OrganizerId must be greater than zero.", nameof(stripeAccountId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventorganizer 
                                 SET  StripeConnectStatus = @StripeAccountStatus 
                                 WHERE CustomerId = @organizerId and
                                 StripeAccountId=@StripeAccountId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StripeAccountId", stripeAccountId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StripeAccountStatus", stripeAccountStatus.ToString() ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@organizerId", organizerId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                { 
                    EventOrganizer? evtOrg = await _cache.GetOnlyAsync<EventOrganizer>(organizerId.ToString());
                    if (evtOrg!=null)
                    {
                        evtOrg.StripeConnectStatus = stripeAccountStatus.ToString();
                        await _cache.SetOnlyAsync<EventOrganizer>(organizerId.ToString(),evtOrg);
                    }
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Stripe account status: {ex.Message}");
                throw;
            }
        }
    }
}