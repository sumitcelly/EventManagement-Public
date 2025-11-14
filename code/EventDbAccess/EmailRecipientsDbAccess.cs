using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using ZXing.OneD.RSS.Expanded.Decoders;

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

        public async Task<List<EmailRecipient>> GetEmailRecipientsByCampaignId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid email campaign ID.", nameof(id));

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailrecipients WHERE emailcampaignid = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            var recipients = new List<EmailRecipient>();
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

        public async Task<Dictionary<string, FullNameOrder>> InsertRecipientsForEvent(int eventId, int emailCampaignId)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            // Select all email addresses for the event and filter by status for salesorder
            var selectQuery = @"SELECT a.Email, a.FullName, a.OrderId FROM User a, salesorder b 
                                WHERE b.EventId = @eventId and 
                                b.UserId = a.UserId";
            using var selectCmd = new MySqlCommand(selectQuery, conn);
            selectCmd.Parameters.AddWithValue("@eventId", eventId);
            var orderUser = new Dictionary<string, FullNameOrder>();

            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    FullNameOrder eu = new()
                    {
                        SalesOrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName"))
                    };
                    orderUser.Add(reader.GetString(reader.GetOrdinal("Email")), eu);
                }
            }

            if (orderUser.Count == 0)
                return orderUser;

            // Build bulk insert statement
            var insertQuery = new System.Text.StringBuilder();
            insertQuery.Append("INSERT INTO emailrecipients (emailcampaignid, recipientemail, status, CreatedAt, TokenGuid) VALUES ");
            var parameters = new List<MySqlParameter>();
            foreach (var ge in orderUser)
            {
                if (ge.Value != null)
                {
                    if (parameters.Count > 0) insertQuery.Append(", ");
                    insertQuery.Append($"(@emailcampaignid, @recipientemail{parameters.Count}, 'Pending', @CreatedAt, UUID())");
                    parameters.Add(new MySqlParameter($"@recipientemail{parameters.Count}", ge.Key));
                }
            }

            parameters.Add(new MySqlParameter("@emailcampaignid", emailCampaignId));
            parameters.Add(new MySqlParameter("@CreatedAt", DateTime.UtcNow));

            using var insertCmd = new MySqlCommand(insertQuery.ToString(), conn);
            insertCmd.Parameters.AddRange(parameters.ToArray());
            int insertedCount = await insertCmd.ExecuteNonQueryAsync();
            return orderUser;
        }
    }

    public class FullNameOrder
    {
        public string FullName { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }

    }
}
