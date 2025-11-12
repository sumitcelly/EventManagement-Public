using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventManagementDbAccess
{


    public class EmailRecipientsDbAccess : BaseDbAccess
    {
        public EmailRecipientsDbAccess(IConfiguration configuration, ILogger<EmailRecipientsDbAccess> logger, IDistributedCache cache) : base(configuration, logger, cache) { }

        public async Task<int> CreateEmailRecipient(EmailRecipient recipient)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"INSERT INTO emailrecipients (emailcampaignid, recipientemail, status, CreatedAt, LastAttemptedAt, RetryCount, TokenGuid) VALUES (@emailcampaignid, @recipientemail, @status, @CreatedAt, @LastAttemptedAt, @RetryCount, @TokenGuid); SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@emailcampaignid", recipient.EmailCampaignId);
            cmd.Parameters.AddWithValue("@recipientemail", recipient.RecipientEmail);
            cmd.Parameters.AddWithValue("@status", recipient.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", recipient.CreatedAt);
            cmd.Parameters.AddWithValue("@LastAttemptedAt", recipient.LastAttemptedAt);
            cmd.Parameters.AddWithValue("@RetryCount", recipient.RetryCount);
            cmd.Parameters.AddWithValue("@TokenGuid", recipient.TokenGuid);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<EmailRecipient?> GetEmailRecipientByCampaignId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid email campaign ID.", nameof(id));

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailrecipients WHERE emailcampaignid = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new EmailRecipient
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    EmailCampaignId = reader.GetInt32(reader.GetOrdinal("emailcampaignid")),
                    RecipientEmail = reader.GetString(reader.GetOrdinal("recipientemail")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    LastAttemptedAt = reader.IsDBNull(reader.GetOrdinal("LastAttemptedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAttemptedAt")),
                    RetryCount = reader.IsDBNull(reader.GetOrdinal("RetryCount")) ? null : reader.GetInt32(reader.GetOrdinal("RetryCount")),
                    TokenGuid = reader.GetString(reader.GetOrdinal("TokenGuid"))
                };
            }
            return null;
        }

        public async Task<List<EmailRecipient>> GetAllEmailRecipients()
        {
            var recipients = new List<EmailRecipient>();
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailrecipients";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recipients.Add(new EmailRecipient
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    EmailCampaignId = reader.GetInt32(reader.GetOrdinal("emailcampaignid")),
                    RecipientEmail = reader.GetString(reader.GetOrdinal("recipientemail")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    LastAttemptedAt = reader.IsDBNull(reader.GetOrdinal("LastAttemptedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAttemptedAt")),
                    RetryCount = reader.IsDBNull(reader.GetOrdinal("RetryCount")) ? null : reader.GetInt32(reader.GetOrdinal("RetryCount")),
                    TokenGuid = reader.GetString(reader.GetOrdinal("TokenGuid"))
                });
            }
            return recipients;
        }

        public async Task<bool> UpdateEmailRecipient(EmailRecipient recipient)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"UPDATE emailrecipients SET emailcampaignid = @emailcampaignid, recipientemail = @recipientemail, status = @status, LastAttemptedAt = @LastAttemptedAt, RetryCount = @RetryCount, TokenGuid = @TokenGuid WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", recipient.Id);
            cmd.Parameters.AddWithValue("@emailcampaignid", recipient.EmailCampaignId);
            cmd.Parameters.AddWithValue("@recipientemail", recipient.RecipientEmail);
            cmd.Parameters.AddWithValue("@status", recipient.Status);
            cmd.Parameters.AddWithValue("@LastAttemptedAt", recipient.LastAttemptedAt);
            cmd.Parameters.AddWithValue("@RetryCount", recipient.RetryCount);
            cmd.Parameters.AddWithValue("@TokenGuid", recipient.TokenGuid);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteEmailRecipient(int id)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "DELETE FROM emailrecipients WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> InsertRecipientsForEvent(int eventId, int emailCampaignId)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            // Select all email addresses for the event
            var selectQuery = "SELECT BuyerEmail FROM EventSalesItem WHERE EventId = @eventId";
            using var selectCmd = new MySqlCommand(selectQuery, conn);
            selectCmd.Parameters.AddWithValue("@eventId", eventId);
            var emails = new List<string>();
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    emails.Add(reader.GetString(reader.GetOrdinal("BuyerEmail")));
                }
            }

            if (emails.Count == 0)
                return 0;

            // Build bulk insert statement
            var insertQuery = new System.Text.StringBuilder();
            insertQuery.Append("INSERT INTO emailrecipients (emailcampaignid, recipientemail, status, CreatedAt, TokenGuid) VALUES ");
            var parameters = new List<MySqlParameter>();
            for (int i = 0; i < emails.Count; i++)
            {
                if (i > 0) insertQuery.Append(", ");
                insertQuery.Append($"(@emailcampaignid, @recipientemail{i}, 'Pending', @CreatedAt, UUID())");
                parameters.Add(new MySqlParameter($"@recipientemail{i}", emails[i]));
            }
            parameters.Add(new MySqlParameter("@emailcampaignid", emailCampaignId));
            parameters.Add(new MySqlParameter("@CreatedAt", DateTime.UtcNow));

            using var insertCmd = new MySqlCommand(insertQuery.ToString(), conn);
            insertCmd.Parameters.AddRange(parameters.ToArray());
            int insertedCount = await insertCmd.ExecuteNonQueryAsync();
            return insertedCount;
        }
    }
}
