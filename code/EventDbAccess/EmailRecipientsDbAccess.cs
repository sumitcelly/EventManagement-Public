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

        /// <summary>
        /// Not  used currently. We do not insert the email recipients until we have an email campaign ready to be sent. At that point we insert all the recipients for the campaign in bulk using InsertRecipientsForEvent which gets the email addresses for all attendees of the event linked to the email campaign.
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
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

        public async Task<bool> BulkUpdateCampaignStatus(List<EmailStatusUpdate> statusUpdates)
        {
            /*
                UPDATE emailrecipients AS er 
                JOIN (
                    SELECT @id0 AS Id, @status0 AS Status, @errorMessage0 AS ErrorMessage, @senderMessageId0 AS SenderMessageId
                    UNION ALL 
                    SELECT @id1 AS Id, @status1 AS Status, @errorMessage1 AS ErrorMessage, @senderMessageId1 AS SenderMessageId
                ) AS updates ON er.id = updates.Id 
                SET er.status = updates.Status, 
                    er.ErrorMessage = updates.ErrorMessage, 
                    er.SenderMessageId = updates.SenderMessageId;
            */
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = new System.Text.StringBuilder();
            query.Append("UPDATE emailrecipients AS er JOIN (");
            for (int i = 0; i < statusUpdates.Count; i++)
            {
                if (i > 0) query.Append(" UNION ALL ");
                query.Append($"SELECT @id{i} AS Id, @status{i} AS Status, @errorMessage{i} AS ErrorMessage, @senderMessageId{i} AS SenderMessageId, @sentAt{i} AS SentAt");
            }
            query.Append(@") AS updates ON er.id = updates.Id SET er.status = updates.Status,
             er.ErrorMessage = updates.ErrorMessage, er.SenderMessageId = updates.SenderMessageId, er.SentAt = updates.SentAt");

            using var cmd = new MySqlCommand(query.ToString(), conn);
            for (int i = 0; i < statusUpdates.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@id{i}", statusUpdates[i].Id);
                cmd.Parameters.AddWithValue($"@status{i}", statusUpdates[i].Status);
                cmd.Parameters.AddWithValue($"@errorMessage{i}", statusUpdates[i].ErrorMessage);
                cmd.Parameters.AddWithValue($"@senderMessageId{i}", statusUpdates[i].SenderMessageId);
                cmd.Parameters.AddWithValue($"@sentAt{i}", statusUpdates[i].SentAt ?? (object)DBNull.Value);
            }

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        public async Task<List<EmailRecipient>> GetEmailRecipientsByCampaignId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid email campaign ID.", nameof(id));

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailrecipients WHERE emailcampaignid = @id and status = 'Pending' or  status= 'QueuingFailure_Retry'";
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
                        LastAttemptedAt = @LastAttemptedAt, RetryCount = @RetryCount, SalesOrderId = @SalesOrderId ,
                        ErrorMessage = @ErrorMessage
                        WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", recipient.Id);
            cmd.Parameters.AddWithValue("@emailcampaignid", recipient.EmailCampaignId);
            cmd.Parameters.AddWithValue("@recipientemail", recipient.RecipientEmail);
            cmd.Parameters.AddWithValue("@status", recipient.Status);
            cmd.Parameters.AddWithValue("@LastAttemptedAt", recipient.LastAttemptedAt);
            cmd.Parameters.AddWithValue("@RetryCount", recipient.RetryCount);
            cmd.Parameters.AddWithValue("@ErrorMessage",recipient.ErrorMessage);
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
        public int UserId { get; internal set; }

        public List<EventSalesItem> TicketDetails {get; set; }= [];
        public decimal SalesOrderTotal { get; internal set; }
        public decimal TotalFees { get; internal set; }
        public decimal PlatformFees { get; internal set; }
    }

    public class OrderEmailDetails
    {
        public string FullName { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }=0;

        public string Email { get; set; } = string.Empty;

        public string SalesOrderCode { get; set; } = string.Empty;
        public decimal SalesOrderTotal { get; internal set; }
    }
}
