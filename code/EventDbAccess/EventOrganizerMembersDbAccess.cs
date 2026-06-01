using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
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
        private static  UserDbAccess userDbAccess;

        public EventOrganizerMembersDbAccess(IConfiguration config, 
                    ILogger<EventOrganizerMembersDbAccess> logger, UserDbAccess userDb, IDistributedCache cache = null)
            : base(config, logger, cache)
        {
            userDbAccess = userDb ?? throw new ArgumentNullException(nameof(userDb));
        }

        public async Task<(int,string)> AddMember(EventOrganizerMembers member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO eventorganizermembers 
                    (CustomerId, UserId, Role, IsActive,InvitationToken)
                    VALUES (@CustomerId, @UserId, @Role, @IsActive,@InvitationToken)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CustomerId", member.CustomerId);
                cmd.Parameters.AddWithValue("@UserId", member.UserId);
                cmd.Parameters.AddWithValue("@Role", member.Role);
                cmd.Parameters.AddWithValue("@IsActive", member.IsActive);
                cmd.Parameters.AddWithValue("@InvitationToken", Guid.NewGuid().ToString());

                
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"Error adding member to orgid {member?.CustomerId}");
                    return (0,string.Empty);
                }
                else
                {
                    // Invalidate cache for this member
                    //todo: look into more efficient cache invalidation strategy if needed, currently we are invalidating the entire cache for members of this customer which might not be optimal if there are many members and frequent changes. We can consider caching individual members or using a more sophisticated caching strategy if performance becomes an issue.
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(member.CustomerId.ToString());
                    var orgMembers = await _cache.GetOnlyAsync<List<EventOrganizerMembers>>(cacheKey);
                    if (orgMembers != null && orgMembers.Count > 0
                        && !orgMembers.Exists(x => x.OrganizerMemberId == member.OrganizerMemberId))
                    {
                        member.OrganizerMemberId = (int)cmd.LastInsertedId;
                        orgMembers.Add(member);
                        await _cache.SetOnlyAsync<List<EventOrganizerMembers>>(cacheKey, orgMembers);
                    }
                    else
                    {
                        _logger.LogInformation($"Cache for customer id {member.CustomerId} is not set or empty, skipping cache update for new member addition.");
                    }
                    
                    _logger.LogInformation($"Member with add  successfully to customer id {member.CustomerId}.");
                     return(Convert.ToInt16(cmd.LastInsertedId), cmd.Parameters["@InvitationToken"].Value.ToString() ?? "");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding member: {ex.Message}");
                throw;
            }
        }

        public async Task<EventOrganizerMembers> GetMemberById(int organizerMemberId, int customerId)
        {
            if (organizerMemberId <= 0)
                throw new ArgumentException("OrganizerMemberId must be greater than zero.", nameof(organizerMemberId));
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));

            string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(customerId.ToString());
            List<EventOrganizerMembers>? orgMembers = await _cache.GetOrSetAsync(cacheKey, () => GetMembersByCustomerIdFromDb(customerId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            if (orgMembers != null)
            {
                var member = orgMembers.Find(x => x.OrganizerMemberId == organizerMemberId);
                if (member != null)
                    return member;
            }
             throw new KeyNotFoundException($"Member with ID {organizerMemberId} not found for customer id {customerId}.");
        }

        /// <summary>
        /// Not used so far
        /// </summary>
        /// <param name="organizerMemberId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<EventOrganizerMembers?> GetMemberByIdFromDb(int organizerMemberId)
        {
            if (organizerMemberId <= 0)
                throw new ArgumentException("OrganizerMemberId must be greater than zero.", nameof(organizerMemberId));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT a.OrganizerMemberId, a.CustomerId, a.UserId, a.Role, a.CreatedAt, a.ModifiedAt, a.IsActive, b.email, b.fullname
                                FROM eventorganizermembers a
                                INNER JOIN eventuser b ON a.UserId = b.UserId
                                WHERE a.OrganizerMemberId = @OrganizerMemberId";
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
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName"))
                      
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
                                 a.userid=b.userid and a.CustomerId = @customerId and a.Role != 'Owner'";

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

        public async Task<bool> SetMemberToActive(int memberId, int customerId)
        {
            if (memberId <= 0)
                throw new ArgumentException("MemberId must be greater than zero.", nameof(memberId));
            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be greater than zero.", nameof(customerId));
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"UPDATE eventorganizermembers 
                                SET IsActive = true, 
                                ModifiedAt = @ModifiedAt,
                                InvitationToken = null
                                WHERE OrganizerMemberId = @orgMemberId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@orgMemberId", memberId);
                cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No member found with OrganizerMemberId {memberId} to activate.");
                    return false;
                }
                else
                {
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(customerId.ToString());
                    var membersList = await _cache.GetOnlyAsync<List<EventOrganizerMembers>>(cacheKey);
                    if (membersList != null && membersList.Count > 0)
                    {
                        var member = membersList.Find(x => x.OrganizerMemberId == memberId);
                        if (member != null)
                        {
                            member.IsActive = true;
                            member.InvitationToken = string.Empty;
                            await _cache.SetOnlyAsync<List<EventOrganizerMembers>>(cacheKey, membersList);
                            _logger.LogInformation($"Updated cache for member id {memberId} to active status.");
                        }
                        else
                        {
                            _logger.LogInformation($"Member with id {memberId} not found in cache to update active status.");
                        }
                    }
                    _logger.LogInformation($"Activated member with OrganizerMemberId {memberId} successfully.");
                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error activating member: {ex.Message}");
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
                                SET  Role = @role ,
                                ModifiedAt = @ModifiedAt
                                WHERE OrganizerMemberId = @orgMemberId and CustomerId = @customerId";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@customerId", member.CustomerId);      
                cmd.Parameters.AddWithValue("@orgMemberId", member.OrganizerMemberId);   
                cmd.Parameters.AddWithValue("@role", member.Role); 
                cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);         
                

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"No member found with OrganizerMemberId {member.OrganizerMemberId} to update.");
                    return false;
                }
                else
                {
                    _logger.LogInformation($"Updated event organizer member permission and role.");
                  
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<List<EventOrganizerMembers>>(member.CustomerId.ToString());
                    var orgMembers  = await _cache.GetOnlyAsync<List<EventOrganizerMembers>>(cacheKey);
                    bool updateUserInfo = false;
                    if (orgMembers != null)
                    {
                        var tempmember = orgMembers.Find(x=>x.OrganizerMemberId == member.OrganizerMemberId);
                        if (tempmember != null )
                        {
                           
                            if (!string.Equals(tempmember.Email, member.Email, StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(tempmember.FullName, member.FullName, StringComparison.Ordinal))
                            {
                                updateUserInfo = true;
                                tempmember.Email = member.Email;
                                tempmember.FullName = member.FullName;
                                _logger.LogInformation($"Member email or full name has changed for member id {member.OrganizerMemberId}, updating user info in database.");
                            }
                            
                            tempmember.Role = member.Role;
                            await _cache.SetOnlyAsync<List<EventOrganizerMembers>>(cacheKey, orgMembers);
                            _logger.LogInformation($"Updated cache for member id {member.OrganizerMemberId} with new email and full name.");
                        }
                    }
                    if (updateUserInfo)
                    {
                        _logger.LogInformation($"Updating user info for member id {member.OrganizerMemberId}   ");
                        bool result = await userDbAccess.UpdateUserBasicsById(new EventUser()
                        {
                            UserId = member.UserId,
                            Name = member.FullName,
                            Email = member.Email,
                        });
                        if (result)
                            _logger.LogInformation($"Result of user update is success");
                        else
                          _logger.LogCritical($"Result of user update is failure");
                    }
                    //no idea why i am removing the cache key here
                    // await _cache.RemoveAsync(cacheKey);
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
                    await _cache.GetOnlyAsync<List<EventOrganizerMembers>>(cacheKey).ContinueWith(task =>
                    {
                        if (task.Result != null)
                        {
                            var membersList = task.Result;
                            var memberToRemove = membersList.Find(x => x.UserId == userId);
                            if (memberToRemove != null)
                            {
                                membersList.Remove(memberToRemove);
                                _cache.SetOnlyAsync<List<EventOrganizerMembers>>(cacheKey, membersList).Wait();
                                _logger.LogInformation($"Removed member with user id {userId} from cache after deletion.");
                            }
                        }
                    });
                    
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

        public async Task<EventOrganizerMembers> GetMemberByInvitationToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Invitation token cannot be null or empty.", nameof(token));

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"SELECT a.OrganizerMemberId, a.CustomerId, a.UserId, a.Role, a.CreatedAt, a.ModifiedAt, a.IsActive, b.email, b.fullname
                                FROM eventorganizermembers a
                                INNER JOIN eventuser b ON a.UserId = b.UserId
                                WHERE a.InvitationToken = @token";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@token", token);

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
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName"))
                       
                    };
                }
                throw new KeyNotFoundException($"No member found with the provided invitation token.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving member by invitation token: {ex.Message}");
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