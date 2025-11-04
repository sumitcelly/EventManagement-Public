using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventManagementDbAccess
{
    public class EventOrganizerMembersDbAccess : BaseDbAccess
    {
        public EventOrganizerMembersDbAccess(IConfiguration config, ILogger<EventOrganizerMembersDbAccess> logger, IDistributedCache cache = null)
            : base(config, logger, cache)
        {
        }

        public async Task<int> AddMember(EventOrganizerMembers member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO eventorganizermembers 
                    (CustomerId, UserId, Role, IsActive)
                    VALUES (@CustomerId, @UserId, @Role, @IsActive)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CustomerId", member.CustomerId);
                cmd.Parameters.AddWithValue("@UserId", member.UserId);
                cmd.Parameters.AddWithValue("@Role", member.Role);
                cmd.Parameters.AddWithValue("@IsActive", member.IsActive);

                
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"Error adding member to orgid {member?.CustomerId}");
                    return 0;
                }
                else
                {
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(member.CustomerId.ToString());
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogInformation($"Member with add  successfully to customer id {member.CustomerId}.");
                    return Convert.ToInt16(cmd.LastInsertedId);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding member: {ex.Message}");
                throw;
            }
        }

        public async Task<EventOrganizerMembers?> GetMemberById(int organizerMemberId)
        {
            if (organizerMemberId <= 0)
                throw new ArgumentException("OrganizerMemberId must be greater than zero.", nameof(organizerMemberId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT * FROM eventorganizermembers WHERE OrganizerMemberId = @OrganizerMemberId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@OrganizerMemberId", organizerMemberId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizerMembers
                    {
                        OrganizerMemberId = reader.GetInt32(reader.GetOrdinal("OrganizerMemberId")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Role = reader.GetString(reader.GetOrdinal("Role")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving member: {ex.Message}");
                throw;
            }
        }


        public async Task<List<EventOrganizerMembers>> GetMembersByCustomerId(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(customerId.ToString());
            List<EventOrganizerMembers>? orgMembers = await _cache.GetOrSetAsync(cacheKey, () => GetMembersByCustomerIdFromDb(customerId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return orgMembers ?? throw new KeyNotFoundException($"Event with ID {customerId} not found.");
        }

        public async Task<List<EventOrganizerMembers>> GetMembersByCustomerIdFromDb(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            var members = new List<EventOrganizerMembers>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT a.UserId,a.Role,a.IsActive,a.OrganizerMemberId, b.email,b.fullname
                                FROM eventorganizermembers a, eventuser b WHERE
                                 a.userid=b.userid and CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    members.Add(new EventOrganizerMembers
                    {
                        OrganizerMemberId=reader.GetInt32(reader.GetOrdinal("OrganizerMemberId")),
                        UserId =reader.GetInt32(reader.GetOrdinal("UserId")),
                        Role = reader.GetString(reader.GetOrdinal("Role")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        FullName  = reader.GetString(reader.GetOrdinal("FullName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    });
                }
                return members;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving members: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateMember(EventOrganizerMembers member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventorganizermembers 
                                SET  Role = @role, 
                                IsActive = @isActive
                    WHERE OrganizerMemberId = @orgMemberId and CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@customerId", member.CustomerId);      
                cmd.Parameters.AddWithValue("@orgMemberId", member.OrganizerMemberId);   
                cmd.Parameters.AddWithValue("@role", member.Role);          
                cmd.Parameters.AddWithValue("@isActive", member.IsActive);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No member found with OrganizerMemberId {member.OrganizerMemberId} to update.");
                    return false;
                }
                else
                {
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(member.CustomerId.ToString());
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogInformation($"Member with OrganizerMemberId {member.OrganizerMemberId} updated successfully.");
                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating member: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteMember(int userId, int customerId)
        {
            if (userId <= 0)
                throw new ArgumentException("OrganizerMemberId must be greater than zero.", nameof(userId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"DELETE FROM eventorganizermembers WHERE userId = @userId and customerId=@customerId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No member found with OrganizerMemberId {userId} to delete.");
                    return false;
                }
                else
                {
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(customerId.ToString());
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogInformation($"Member with OrganizerMemberId {customerId} deleted successfully.");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting member: {ex.Message}");
                throw;
            }
        }

        public async Task<EventOrganizerMembers?> GetContainingOrgByUserId(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                string query = @"SELECT * FROM eventorganizermembers WHERE UserId = @UserId";
                using var cmd = new MySqlCommand(query, connection);
         
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EventOrganizerMembers
                    {
                        OrganizerMemberId = reader.GetInt32(reader.GetOrdinal("OrganizerMemberId")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Role = reader.GetString(reader.GetOrdinal("Role")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving member by CustomerId and UserId: {ex.Message}");
                throw;
            }
        }
    }
}