using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventManagementDbAccess
{


    public class EmailCampaignDbAccess : BaseDbAccess
    {
        public EmailCampaignDbAccess(IConfiguration configuration, ILogger<EmailCampaignDbAccess> logger, IDistributedCache cache) :base(configuration, logger, cache)
        {
        
        }

        public async Task<int> CreateEmailCampaign(EmailCampaign campaign)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"INSERT INTO emailcampaign (TemplateId, EventId, SendAt, Status,Name,Description, CreatedAt, ModifiedAt) 
                        VALUES (@TemplateId, @EventId, @SendAt, @Status,@Name,@Description, @CreatedAt, @ModifiedAt); 
                        SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TemplateId", campaign.TemplateId);
            cmd.Parameters.AddWithValue("@EventId", campaign.EventId);
            cmd.Parameters.AddWithValue("@SendAt", campaign.SendAt);
            cmd.Parameters.AddWithValue("@Status", campaign.Status);
            cmd.Parameters.AddWithValue("@Name", campaign.Name);
            cmd.Parameters.AddWithValue("@Description", campaign.Description);
            cmd.Parameters.AddWithValue("@CreatedAt",DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<EmailCampaign>> GetPendingCampaigns()
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailcampaign WHERE Status = 'Pending'  and  SendAt <= Utc_timestamp()";
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
                    ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
                });
            }
            return campaigns;       
        }

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
                    ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"))
                });
            }
            return campaigns;
        }


        public async Task<EmailCampaign> GetEmailCampaignByCampaignId(int campaignId)
        {
            if (campaignId <= 0)
            {
                throw new ArgumentException("EventId must be greater than zero.", nameof(campaignId));
            }

            
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"SELECT a.Id, a.templateid,a.eventid,a.sendat,a.status,a.Name,a.Description,
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
            var query = @"select a.Id, c.EventName, a.SendAt, a.Status, a.Name,a.Description,
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
                    TemplateId = reader.GetInt32(reader.GetOrdinal("TemplateId")),
                    TemplateName = reader.GetString(reader.GetOrdinal("TemplateName")),   
                    Name= reader.GetString(reader.GetOrdinal("Name")),
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
            return rows > 0;
        }
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
            return rows > 0;
        }
    }
}
