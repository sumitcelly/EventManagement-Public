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
            var query = @"INSERT INTO emailrecipients (emailcampaignid, recipientemail, status, CreatedAt, LastAttemptedAt, RetryCount, SalesOrderId) VALUES (@emailcampaignid, @recipientemail, @status, @CreatedAt, @LastAttemptedAt, @RetryCount, @TokenGuid); SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@emailcampaignid", recipient.EmailCampaignId);
            cmd.Parameters.AddWithValue("@recipientemail", recipient.RecipientEmail);
            cmd.Parameters.AddWithValue("@status", recipient.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", recipient.CreatedAt);
            cmd.Parameters.AddWithValue("@LastAttemptedAt", recipient.LastAttemptedAt);
            cmd.Parameters.AddWithValue("@RetryCount", recipient.RetryCount);
            cmd.Parameters.AddWithValue("@SalesOrderId", recipient.SalesOrderId);
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
                    SalesOrderId = reader.GetInt16(reader.GetOrdinal("SalesOrderId"))
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
                    SalesOrderId = reader.GetInt16(reader.GetOrdinal("SalesOrderId"))
                });
            }
            return recipients;
        }

        public async Task<bool> UpdateEmailRecipient(EmailRecipient recipient)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"UPDATE emailrecipients SET emailcampaignid = @emailcampaignid, recipientemail = @recipientemail, status = @status, 
                        LastAttemptedAt = @LastAttemptedAt, RetryCount = @RetryCount, SalesOrderId = @SalesOrderId 
                        WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", recipient.Id);
            cmd.Parameters.AddWithValue("@emailcampaignid", recipient.EmailCampaignId);
            cmd.Parameters.AddWithValue("@recipientemail", recipient.RecipientEmail);
            cmd.Parameters.AddWithValue("@status", recipient.Status);
            cmd.Parameters.AddWithValue("@LastAttemptedAt", recipient.LastAttemptedAt);
            cmd.Parameters.AddWithValue("@RetryCount", recipient.RetryCount);
            cmd.Parameters.AddWithValue("@SalesOrderId", recipient.SalesOrderId);
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

        public async Task<List<OrderEmailDetails>> InsertRecipientsForEvent(int eventId, int emailCampaignId)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            // Select all email addresses for the event and filter by status for salesorder
            var selectQuery = @"SELECT a.Email, a.FullName, b.OrderId FROM eventuser a, salesorder b 
                                WHERE b.EventId = @eventId and 
                                b.UserId = a.UserId";
            using var selectCmd = new MySqlCommand(selectQuery, conn);
            selectCmd.Parameters.AddWithValue("@eventId", eventId);
            var orderUser = new List<OrderEmailDetails>();

            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    OrderEmailDetails eu = new()
                    {
                        SalesOrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email"))
                    };
                    orderUser.Add(eu);
                }
            }

            if (orderUser.Count == 0)
                return orderUser;

            // Build bulk insert statement
            var insertQuery = new System.Text.StringBuilder();
            insertQuery.Append("INSERT INTO emailrecipients (emailcampaignid, recipientemail, status, CreatedAt, SalesOrderId) VALUES ");
            var parameters = new List<MySqlParameter>();
            foreach (var tempUser in orderUser)
            {
                
                if (parameters.Count > 0) insertQuery.Append(", ");
                int count1 = parameters.Count;
                insertQuery.Append($"(@emailcampaignid, @recipientemail{count1}, 'Pending', @CreatedAt, @SalesOrderId{count1})");
                parameters.Add(new MySqlParameter($"@recipientemail{count1}", tempUser.Email));       
                parameters.Add(new MySqlParameter($"@SalesOrderId{count1}", tempUser.SalesOrderId));  
                     
            }

            parameters.Add(new MySqlParameter("@emailcampaignid", emailCampaignId));
            parameters.Add(new MySqlParameter("@CreatedAt", DateTime.UtcNow));

            using var insertCmd = new MySqlCommand(insertQuery.ToString(), conn);
            insertCmd.Parameters.AddRange(parameters.ToArray());
            int insertedCount = await insertCmd.ExecuteNonQueryAsync();
            return orderUser;
        }
    }

    public class DecryptedOrderDetails
    {
        public int SalesOrderId { get; set; }

        public string SalesOrderStatus { get; set; } = string.Empty;

        public int EventId { get; set; }

        public string SalesOrderCode { get; set; } = string.Empty;
    }

    public class OrderEmailDetails
    {
        public string FullName { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }

        public string Email { get; set; } = string.Empty;

    }
}
