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
using System.Text.Unicode;
namespace EventManagementDbAccess
{

    
    /// <summary>
    /// this class has 2 redis caches. One has a key of EmailTemplate:_default and value of list of default templates. Second cache has key of EmailTemplate:Id and value of template content and subject. Third cache has key of EmailTemplate:TemplateName and value of template content and subject. So when we add or update a template we need to invalidate all 3 caches if the template is default otherwise only second and third cache.
    ///  and finally there is a cache with key of EmailTemplate:Id and value of template content and subject. So when we add or update a template we need to invalidate all 3 caches if the template is default otherwise only second and third cache.
    /// </summary>
    public class NotificationTemplateAccess : BaseDbAccess
    {
        public static readonly string EventReminder7DayTemplateName = "EventReminder7day";
        public static readonly string EventReminder2DayTemplateName = "EventReminder2day";
        public static readonly string EmailVerificationTemplateName = "EmailVerification";
        public static readonly string OrderConfirmationTemplateName = "OrderConfirmation";
        
        public NotificationTemplateAccess(IConfiguration connectionString, ILogger<NotificationTemplateAccess> logger, IDistributedCache cache) : base(connectionString, logger, cache)
        {
        }

       

        public async Task<List<int>> GetDefaultTemplatesIds()
        {
            string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>("_default");
            List<EmailTemplate>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetDefaultTemplatesFromDb(), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            if (templateData == null || templateData.Count == 0)
            {
                return [];
            }
            else
            {
                return templateData.Select(x => x.Id).ToList();
            }
        }

        public async Task<List<EmailTemplate>> GetDefaultTemplates()
        {
           
            string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>("_default");
            List<EmailTemplate>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetDefaultTemplatesFromDb(), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template with _default id not found.");
        }

        public async Task<List<EmailTemplate>> GetDefaultTemplatesFromDb()
        {
            try
            {
                List<EmailTemplate> templates = new List<EmailTemplate>();
                using (MySqlConnection connection = new(this.ConnectionString))
                {
                    string sql = @$"Select Id,TemplateName,TemplateDescription,TemplateContent, Subject from  eventmanagement.notificationtemplates where
                                    IsDefault=1";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using DbDataReader reader = await cmd.ExecuteReaderAsync();
                    
                    
                    while (await reader.ReadAsync())
                    {
                        templates.Add(new EmailTemplate()
                        {
                            Id = reader.GetInt32(0),
                          
                            TemplateName = reader.GetString(1),
                            TemplateDescription = reader.IsDBNull(2) ? null : reader.GetString(2),
                            TemplateContent=reader.GetString(3),
                            Subject=reader.GetString(4)
                        });
                    }
                    
                }
                return templates;
            }
            catch (Exception ex)
            {
               _logger.LogCritical("Could not retrive templates ids for default {0}",ex);
               throw;
            }
        }
        public async Task<Tuple<string, string,bool>> GetTemplateById(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentNullException("templateId");
            }

            List<EmailTemplate> templates= await GetDefaultTemplates();
            if (templates?.Count>0)
            {
                var template = templates.FirstOrDefault(t => t.Id == templateId);
                if (template != null)
                {
                    return new (template.TemplateContent, template.Subject,true);
                }
            }
            string cacheKey = CacheHelper.GetCacheKey<EmailTemplate>(templateId.ToString());
            Tuple<string, string>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetTemplateByIdFromDb(templateId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return new (templateData?.Item1 ?? string.Empty,templateData?.Item2 ?? string.Empty,false);
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
                                    Id={templateId}";
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
                _logger.LogError($"Error retrieving template with id {templateId} from database: {ex.Message}");
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
        public async Task<Tuple<string, string>> GetDefaultTemplateDetailsByName(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException(nameof(templateName));
            }
            var defaultList = await GetDefaultTemplates();
            var template = defaultList.FirstOrDefault(t => t.TemplateName == templateName);
            if (template == null)
            {
                throw new KeyNotFoundException($"Default template with name {templateName} not found.");
            }
            else
            {
                return new Tuple<string,string>(template.TemplateContent, template.Subject);
            }
           
        }
    

        /// <summary>
        /// Not used currently. Since we only retrive by id or by default name
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
                cmd.Parameters.AddWithValue("@templateContent", string.IsNullOrWhiteSpace(template.TemplateContent)?
                                                                string.Empty:
                                                                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(template.TemplateContent)));
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
                cmd.Parameters.AddWithValue("@templateContent", string.IsNullOrWhiteSpace(template.TemplateContent)?
                                                                string.Empty:
                                                                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(template.TemplateContent)));
            
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