using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventManagementDbAccess
{
    public class EmailTransactionLogDbAccess : BaseDbAccess
    {
        public EmailTransactionLogDbAccess(IConfiguration configuration, ILogger<EmailTransactionLogDbAccess> logger, IDistributedCache cache) : base(configuration, logger, cache) { }

        /// <summary>
        /// Inserts a new email transaction log record into the database
        /// </summary>
        /// <param name="transactionLog">The email transaction log object to insert</param>
        /// <returns>The ID of the newly inserted record</returns>
        public async Task<int> InsertEmailTransactionLog(EmailTransactionLog transactionLog)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = @"INSERT INTO emailtransactionlog (recipientemail, emailtype, salesorderid, sendermessageid, status, createdat, sentat, errormessage) 
                         VALUES (@recipientemail, @emailtype, @salesorderid, @sendermessageid, @status, @createdat, @sentat, @errormessage); 
                         SELECT LAST_INSERT_ID();";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@recipientemail", transactionLog.RecipientEmail);
            cmd.Parameters.AddWithValue("@emailtype", transactionLog.EmailType);
            cmd.Parameters.AddWithValue("@salesorderid", transactionLog.SalesOrderId);
            cmd.Parameters.AddWithValue("@sendermessageid", transactionLog.SenderMessageId);
            cmd.Parameters.AddWithValue("@status", transactionLog.Status);
            cmd.Parameters.AddWithValue("@createdat", transactionLog.CreatedAt);
            cmd.Parameters.AddWithValue("@sentat", transactionLog.SentAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@errormessage", transactionLog.ErrorMessage);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Retrieves all email transaction logs for a specific sales order
        /// </summary>
        /// <param name="salesOrderId">The sales order ID to filter by</param>
        /// <returns>List of email transaction logs for the specified sales order</returns>
        public async Task<List<EmailTransactionLog>> GetEmailTransactionLogsBySalesOrderId(int salesOrderId)
        {
            if (salesOrderId <= 0)
                throw new ArgumentException("Invalid sales order ID.", nameof(salesOrderId));

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailtransactionlog WHERE salesorderid = @salesorderid";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@salesorderid", salesOrderId);
            using var reader = await cmd.ExecuteReaderAsync();
            
            var transactionLogs = new List<EmailTransactionLog>();
            while (await reader.ReadAsync())
            {
                transactionLogs.Add(MapReaderToEmailTransactionLog(reader));
            }

            return transactionLogs;
        }

        /// <summary>
        /// Retrieves all email transaction logs for a specific email address
        /// </summary>
        /// <param name="recipientEmail">The recipient email address to filter by</param>
        /// <returns>List of email transaction logs for the specified email address</returns>
        public async Task<List<EmailTransactionLog>> GetEmailTransactionLogsByEmail(string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
                throw new ArgumentException("Email address cannot be null or empty.", nameof(recipientEmail));

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "SELECT * FROM emailtransactionlog WHERE recipientemail = @recipientemail";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@recipientemail", recipientEmail);
            using var reader = await cmd.ExecuteReaderAsync();
            
            var transactionLogs = new List<EmailTransactionLog>();
            while (await reader.ReadAsync())
            {
                transactionLogs.Add(MapReaderToEmailTransactionLog(reader));
            }

            return transactionLogs;
        }

        /// <summary>
        /// Bulk updates email transaction logs with status, sent time, and error information
        /// </summary>
        /// <param name="statusUpdates">List of transaction log status updates</param>
        /// <returns>True if any rows were affected</returns>
        public async Task<bool> BulkUpdateTransactionLogs(List<EmailTransactionStatusUpdate> statusUpdates)
        {
            if (statusUpdates == null || statusUpdates.Count == 0)
                return false;

            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = new System.Text.StringBuilder();
            query.Append("UPDATE emailtransactionlog AS etl JOIN (");
            for (int i = 0; i < statusUpdates.Count; i++)
            {
                if (i > 0) query.Append(" UNION ALL ");
                query.Append($"SELECT @id{i} AS Id, @status{i} AS Status, @errorMessage{i} AS ErrorMessage, @senderMessageId{i} AS SenderMessageId, @sentAt{i} AS SentAt");
            }
            query.Append(") AS updates ON etl.id = updates.Id SET etl.status = updates.Status, etl.errormessage = updates.ErrorMessage, etl.sendermessageid = updates.SenderMessageId, etl.sentat = updates.SentAt");

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

        /// <summary>
        /// Maps a data reader row to an EmailTransactionLog object
        /// </summary>
        private EmailTransactionLog MapReaderToEmailTransactionLog(DbDataReader reader)
        {
            return new EmailTransactionLog
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                RecipientEmail = reader.GetString(reader.GetOrdinal("recipientemail")),
                EmailType = reader.GetString(reader.GetOrdinal("emailtype")),
                SalesOrderId = reader.GetInt32(reader.GetOrdinal("salesorderid")),
                SenderMessageId = reader.GetString(reader.GetOrdinal("sendermessageid")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                SentAt = reader.IsDBNull(reader.GetOrdinal("sentat")) ? null : reader.GetDateTime(reader.GetOrdinal("sentat")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("errormessage")) ? string.Empty : reader.GetString(reader.GetOrdinal("errormessage"))
            };
        }
    }
}
