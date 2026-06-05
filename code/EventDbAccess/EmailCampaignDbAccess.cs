using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventManagementDbAccess
{


    public class EmailCampaignDbAccess : BaseDbAccess
    {
        public EmailCampaignDbAccess(IConfiguration configuration,
                                     ILogger<EmailCampaignDbAccess> logger, 
                                    IDistributedCache cache) :
                                    base(configuration, logger, cache)
        {
        
        }

        public async Task<int> CreateEmailCampaign(EmailCampaign campaign)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"INSERT INTO emailcampaign (TemplateId, EventId, SendAt, Status,Name,Description,Enabled, CreatedAt, ModifiedAt) 
                        VALUES (@TemplateId, @EventId, @SendAt, @Status,@Name,@Description, @Enabled,@CreatedAt, @ModifiedAt); 
                        SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TemplateId", campaign.TemplateId);
            cmd.Parameters.AddWithValue("@EventId", campaign.EventId);
            cmd.Parameters.AddWithValue("@SendAt", campaign.SendAt);
            cmd.Parameters.AddWithValue("@Status", campaign.Status);
            cmd.Parameters.AddWithValue("@Name", campaign.Name);
            cmd.Parameters.AddWithValue("@Description", campaign.Description);
            cmd.Parameters.AddWithValue("@Enabled",1);
            cmd.Parameters.AddWithValue("@CreatedAt",DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

            var result = await cmd.ExecuteScalarAsync();
            int id=  Convert.ToInt32(result);
            campaign.Id = id;

            string key = CacheHelper.GetCacheKey<EmailCampaign>(id.ToString());
            await _cache.SetOnlyAsync(key, campaign);

            return id;
        }

        public async Task<List<EmailCampaign>> GetPendingCampaigns()
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailcampaign WHERE (Status = 'Pending' or Status='Incomplete')  and Enabled = 1 and SendAt <= Utc_timestamp()";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var campaigns = new List<EmailCampaign>();
            while (await reader.ReadAsync())
            {
                campaigns.Add(new EmailCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name= reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))? string.Empty : reader.GetString(reader.GetOrdinal("Description")),

                    TemplateId = reader.GetInt32(reader.GetOrdinal("TemplateId")),
                    EventId = reader.IsDBNull(reader.GetOrdinal("EventId")) ? null : reader.GetInt32(reader.GetOrdinal("EventId")),
                    SendAt = reader.GetDateTime(reader.GetOrdinal("SendAt")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                    Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled"))
                });
            }
            return campaigns;       
        }

        /// <summary>
        /// Not being used
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>/
        public async Task<List<EmailCampaign>> GetEmailCampaignsByEventId(int eventId)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException("EventId must be greater than zero.", nameof(eventId));
            }

            var campaigns = new List<EmailCampaign>();
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailcampaign where eventId = @EventId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@EventId", eventId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                campaigns.Add(new EmailCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name= reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    TemplateId = reader.GetInt32(reader.GetOrdinal("TemplateId")),
                    EventId = reader.IsDBNull(reader.GetOrdinal("EventId")) ? null : reader.GetInt32(reader.GetOrdinal("EventId")),
                    SendAt = reader.GetDateTime(reader.GetOrdinal("SendAt")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt")),
                    Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled"))
                });
            }
            return campaigns;
        }
        public async Task<EmailCampaign> GetEmailCampaignByCampaignId(int campaignId)
        {
            if (campaignId < 0)
            {
                throw new ArgumentException("CampaignId must be greater than zero.", nameof(campaignId));
            }
            string key = CacheHelper.GetCacheKey<EmailCampaign>(campaignId.ToString());
            var cachedCampaign = await _cache.GetOrSetAsync<EmailCampaign>(key,()=> GetEmailCampaignByCampaignIdFromDb(campaignId));
            return cachedCampaign ?? throw new Exception($"Unable to find campaign for id {campaignId}");
        }

        public async Task<EmailCampaign> GetEmailCampaignByCampaignIdFromDb(int campaignId)
        {
            if (campaignId <= 0)
            {
                throw new ArgumentException("EventId must be greater than zero.", nameof(campaignId));
            }
   
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"SELECT a.Id, a.templateid,a.eventid,a.sendat,a.status,a.Name,a.Description,a.Enabled,
                          b.Templatename,b.templatecontent,b.templatedescription,b.subject,b.isdefault,
                          c.eventname
                          FROM emailcampaign a, notificationtemplates b, events c
                          where a.templateid =b.id and a.eventid=c.eventid and 
                          a.id = @campaignid";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@campaignid", campaignId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new EmailCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name= reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled")),
                    TemplateId = reader.GetInt32(reader.GetOrdinal("TemplateId")),
                    EventId = reader.IsDBNull(reader.GetOrdinal("EventId")) ? null : reader.GetInt32(reader.GetOrdinal("EventId")),
                    SendAt = reader.GetDateTime(reader.GetOrdinal("SendAt")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                    EventName = reader.GetString(reader.GetOrdinal("EventName")),
                    TemplateContent = reader.IsDBNull(reader.GetOrdinal("templatecontent"))?null
                                        :System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.GetString(reader.GetOrdinal("templatecontent")))),
                    TemplateDescription = reader.GetString(reader.GetOrdinal("templatedescription")),
                    Subject = reader.GetString(reader.GetOrdinal("Subject"))  ,
                    IsDefault = reader.GetBoolean(reader.GetOrdinal("isdefault")),              

                };
            }
           throw new Exception($"Unable to find campaign for id {campaignId}");
        }

        public async Task<List<EmailCampaign>> GetEmailCampaignsByOrganizerId(int organizerId)
        {
            if (organizerId <= 0)
            {
                throw new ArgumentException("organizerId must be greater than zero.", nameof(organizerId));
            }

            var campaigns = new List<EmailCampaign>();
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"select a.Id, c.EventName,c.EventId, a.SendAt, a.Status, a.Name,a.Description,a.Enabled,
                        b.templatename, b.Id as TemplateId, b.IsDefault  from 
                        emailcampaign a
                        JOIN notificationtemplates b ON a.TemplateId = b.id
                        JOIN events c ON a.EventId = c.EventId
                        WHERE a.eventid IN (SELECT eventid FROM events WHERE eventorganizer = @organizerId)";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@organizerId", organizerId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                campaigns.Add(new EmailCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EventId = reader.IsDBNull(reader.GetOrdinal("EventId")) ? null : reader.GetInt32(reader.GetOrdinal("EventId")),
                    TemplateId = reader.GetInt32(reader.GetOrdinal("TemplateId")),
                    TemplateName = reader.GetString(reader.GetOrdinal("TemplateName")),   
                    Name= reader.GetString(reader.GetOrdinal("Name")),
                    Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),         
                    EventName = reader.IsDBNull(reader.GetOrdinal("EventName")) ? string.Empty : reader.GetString(reader.GetOrdinal("EventName")),
                    SendAt = reader.GetDateTime(reader.GetOrdinal("SendAt")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                });
            }
            return campaigns;
        }

        public async Task<bool> UpdateEmailCampaign(EmailCampaign campaign)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"UPDATE emailcampaign SET EventId = @EventId, SendAt = @SendAt, Status = @Status, TemplateId = @TemplateId,
                        ModifiedAt = @ModifiedAt, Name=@Name, Description=@Description
                         WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", campaign.Id);
            cmd.Parameters.AddWithValue("@EventId", campaign.EventId);
            cmd.Parameters.AddWithValue("@Name", campaign.Name);
            cmd.Parameters.AddWithValue("@Description", campaign.Description);
            cmd.Parameters.AddWithValue("@SendAt", campaign.SendAt);
            cmd.Parameters.AddWithValue("@TemplateId", campaign.TemplateId);
            cmd.Parameters.AddWithValue("@Status", campaign.Status);
            cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
            var rows = await cmd.ExecuteNonQueryAsync();

            string key = CacheHelper.GetCacheKey<EmailCampaign>(campaign.Id.ToString());
            EmailCampaign? cacheObj= await _cache.GetOnlyAsync<EmailCampaign>(key);
            if (cacheObj != null)
            {
                cacheObj.EventId = campaign.EventId;
                cacheObj.Name = campaign.Name;
                cacheObj.Description = campaign.Description;
                cacheObj.SendAt = campaign.SendAt;
                cacheObj.TemplateId = campaign.TemplateId;
                cacheObj.Status = campaign.Status;
                cacheObj.ModifiedAt = DateTime.UtcNow;
                cacheObj.Enabled = campaign.Enabled;
                cacheObj.TemplateContent = campaign.TemplateContent;
                cacheObj.Subject = campaign.Subject;
                cacheObj.TemplateDescription = campaign.TemplateDescription;
                cacheObj.IsDefault = campaign.IsDefault;
                await _cache.SetOnlyAsync(key, cacheObj);
            }

            return rows > 0;
        }

        /// <summary>
        /// Leave this alone from a caching standpoint since we call this from the service
        /// </summary>
        /// <param name="campaignId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<bool> UpdateEmailCampaignStatus(int campaignId, string status)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"UPDATE emailcampaign SET  Status = @Status, ModifiedAt = @ModifiedAt WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);    
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Id", campaignId);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        public async Task<bool> DeleteEmailCampaign(int id)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "DELETE FROM emailcampaign WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var rows = await cmd.ExecuteNonQueryAsync();
            string key = CacheHelper.GetCacheKey<EmailCampaign>(id.ToString());
            await _cache.RemoveAsyncHelper(key);

            return rows > 0;
        }

        public async Task<bool> UpdateEmailCampaignsStatusForEvent(int eventId, bool enable)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            conn.Open();
            int i= enable ? 1 : 0;
            var query = $"UPDATE emailcampaign SET Enabled = {i} WHERE eventId = @eventId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@eventId", eventId);
            
            var rows = await cmd.ExecuteNonQueryAsync();
            return  rows > 0;
        }

        public async Task<bool> CheckIfCamaignsExistForEvent(int eventId)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT COUNT(*) FROM emailcampaign WHERE eventId = @eventId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@eventId", eventId);
            var count = Convert.ToInt32( await cmd.ExecuteScalarAsync());
            return count > 0;
        }

    }
}
