using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;


using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using System.Threading.Tasks;
using System.Data.Common;
using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EventUtils;
using Microsoft.Extensions.Caching.Distributed;
namespace EventManagementDbAccess
{
    public class NotificationTemplateAccess : BaseDbAccess
    {
        public static readonly string[] _defaultTemplateName=new string[] { "EventReminder5Day", "EventReminder1Day",};
        public NotificationTemplateAccess(IConfiguration connectionString, ILogger<NotificationTemplateAccess> logger, IDistributedCache cache) : base(connectionString, logger, cache)
        {
        }

        public async Task<Tuple<string, string>> GetTemplateById(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentNullException("templateId");
            }

            string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>(templateId.ToString());
            Tuple<string, string>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetTemplateByIdFromDb(templateId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template with id {templateId} not found.");
        }

        public async Task<List<int>> GetDefaultTemplatesIds()
        {
           
            string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>("_default");
            List<int>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetDefaultTemplateIdsFromDb(), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template with _default id not found.");
        }

        public async Task<List<int>> GetDefaultTemplateIdsFromDb()
        {
            try
            {
                List<int> templateIds = new List<int>();
                using (MySqlConnection connection = new(this.ConnectionString))
                {
                    string sql = @$"Select Id from  eventmanagement.notificationtemplates where
                                    IsDefault=1";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using DbDataReader reader = await cmd.ExecuteReaderAsync();
                    
                    
                    while (await reader.ReadAsync())
                    {
                        templateIds.Add(reader.GetInt16(0));
                    }
                    
                }
                return templateIds;
            }
            catch (Exception ex)
            {
               _logger.LogCritical("Could not retrive templates ids for default {0}",ex);
               throw;
            }
        }

        public async Task<Tuple<string, string>> GetTemplateByIdFromDb(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentNullException("templateId");
            }

            string templateContent = string.Empty;
            string subject = string.Empty;

            try
            {
                using (MySqlConnection connection = new(this.ConnectionString))
                {
                    string sql = @$"Select TemplateContent,Subject from eventmanagement.notificationtemplates where
                                    Id='{templateId}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using DbDataReader reader = await cmd.ExecuteReaderAsync();
                    if (reader.RecordsAffected > 1)
                        throw new Exception("More than one record returned for template Id " + templateId);

                    while (await reader.ReadAsync())
                    {
                        templateContent = reader.GetString(0);
                        subject = reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new Tuple<string, string>(templateContent, subject);
        }

        public async Task<bool> DeleteTemplate(int id)
        {
            using var conn = new MySqlConnection(this.ConnectionString);
            await conn.OpenAsync();
            var query = "DELETE FROM notificationtemplates WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        public async Task<Tuple<string, string>> GetTemplateByName(string templateName, int? customerId = null)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException("templateName");
            }

            string cacheKey = CacheHelper.GetCacheKey<Event>(templateName.ToString());
            Tuple<string, string>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetTemplateByNameFromDb(templateName), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template  with name  {templateName} not found.");
        }

        public async Task<Tuple<string, string>> GetTemplateByNameFromDb(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException("templateName");
            }

            string templateContent = string.Empty;
            string subject = string.Empty;

            try
            {

                using (MySqlConnection connection = new(this.ConnectionString))
                {
                    string sql = @$"Select TemplateContent,Subject from eventmanagement.notificationtemplates where
                                    TemplateName='{templateName}'";
                                    
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using DbDataReader reader = await cmd.ExecuteReaderAsync();
                    if (reader.RecordsAffected > 1)
                        throw new Exception("More than one record returned for template Name" + templateName);

                    while (await reader.ReadAsync())
                    {
                        templateContent = reader.GetString(0);
                        subject = reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new Tuple<string, string>(templateContent, subject);
        }

        public async Task<int> AddEmailTemplate(EmailTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("Exception adding templated",nameof(template));
            }
             try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO notificationtemplates 
                    (TemplateName, TemplateContent, TemplateDescription,Subject,IsDefault,CreatedAt,ModifiedAt)
                    VALUES (@templateName, @templateContent, @templateDescription, @subject,@isDefault,@createdat,@modifiedat)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@templateName", template.TemplateName);
                cmd.Parameters.AddWithValue("@templateContent", template.TemplateContent);
                cmd.Parameters.AddWithValue("@templateDescription", template.TemplateDescription);
                cmd.Parameters.AddWithValue("@subject", template.Subject);
                cmd.Parameters.AddWithValue("@isDefault", template.IsDefault);
                cmd.Parameters.AddWithValue("@createdat",DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@modifiedat",DateTime.UtcNow);

                
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"Error adding template {template.TemplateName}");
                    return 0;
                }
                else
                {
                    template.Id = Convert.ToInt16(cmd.LastInsertedId);
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>(cmd.LastInsertedId.ToString());
                    await _cache.SetOnlyAsync<EmailTemplate>(cacheKey, template);
                    _logger.LogInformation($"Templated with id {template.Id} has  been created.");
                    return Convert.ToInt16(cmd.LastInsertedId);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding template: {ex.Message}");
                throw;
            }

        }

        public async Task<int> UpdateEmailTemplate(EmailTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException("Exception adding templated",nameof(template));
            }
            if (template.Id <=0)
            {
                throw new ArgumentException("Invalid id of email template to update");
        
            }
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                string query = @"update notificationtemplates 
                                set templateName=@templateName,
                                TemplateContent=@templateContent,
                                TemplateDescription=@templateDescription, 
                                Subject=@subject,
                                ModifiedAt=@modifiedat
                                where id=@templateid";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@templateName", template.TemplateName);
                cmd.Parameters.AddWithValue("@templateContent", template.TemplateContent);
                cmd.Parameters.AddWithValue("@templateDescription", template.TemplateDescription);
                cmd.Parameters.AddWithValue("@subject", template.Subject);        
                cmd.Parameters.AddWithValue("@modifiedat",DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@templateId",template.Id);

                
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"Error updating template {template.TemplateName}");
                    return 0;
                }
                else
                {
                    
                    // Invalidate cache for this member
                    string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>(template.Id.ToString());
                    await _cache.SetOnlyAsync<EmailTemplate>(cacheKey, template);
                    _logger.LogInformation($"Templated with id {template.Id} has  been updated.");
                    return rowsAffected;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding template: {ex.Message}");
                throw;
            }

        }
    }
}