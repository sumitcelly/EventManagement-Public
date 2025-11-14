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
            var query = @"INSERT INTO emailcampaign (TemplateId, EventId, SendAt, Status, CreatedAt, ModifiedAt) VALUES (@TemplateId, @EventId, @SendAt, @Status, @CreatedAt, @ModifiedAt); 
                        SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TemplateId", campaign.TemplateId);
            cmd.Parameters.AddWithValue("@EventId", campaign.EventId);
            cmd.Parameters.AddWithValue("@SendAt", campaign.SendAt);
            cmd.Parameters.AddWithValue("@Status", campaign.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", campaign.CreatedAt);
            cmd.Parameters.AddWithValue("@ModifiedAt", campaign.ModifiedAt);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<EmailCampaign>> GetPendingCampaigns()
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailcampaign WHERE Status = 'Pending' and  SendAt <= Utc_timestamp()";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var campaigns = new List<EmailCampaign>();
            while (await reader.ReadAsync())
            {
                campaigns.Add(new EmailCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
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

        public async Task<bool> UpdateEmailCampaign(EmailCampaign campaign)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"UPDATE emailcampaign SET TemplateId = @TemplateId, EventId = @EventId, SendAt = @SendAt, Status = @Status, ModifiedAt = @ModifiedAt WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", campaign.Id);
            cmd.Parameters.AddWithValue("@TemplateId", campaign.TemplateId);
            cmd.Parameters.AddWithValue("@EventId", campaign.EventId);
            cmd.Parameters.AddWithValue("@SendAt", campaign.SendAt);
            cmd.Parameters.AddWithValue("@Status", campaign.Status);
            cmd.Parameters.AddWithValue("@ModifiedAt", campaign.ModifiedAt);
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
