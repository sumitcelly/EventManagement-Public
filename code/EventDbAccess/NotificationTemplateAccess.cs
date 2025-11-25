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
        public NotificationTemplateAccess(IConfiguration connectionString, ILogger<NotificationTemplateAccess> logger, IDistributedCache cache) : base(connectionString, logger, cache)
        {
        }

        public async Task<Tuple<string, string>> GetTemplateById(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentNullException("templateId");
            }

            string cacheKey = CacheHelper.GetCacheKey<Event>(templateId.ToString());
            Tuple<string, string>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetTemplateByIdFromDb(templateId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template with id {templateId} not found.");
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

        public async Task<Tuple<string, string>> GetTemplateByName(string templateName, int? customerId = null)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException("templateName");
            }

            string cacheKey = customerId.HasValue ? CacheHelper.GetCacheKey<Event>(customerId + ":" + templateName.ToString())
                                                    : CacheHelper.GetCacheKey<Event>(templateName.ToString());
            Tuple<string, string>? templateData = await _cache.GetOrSetAsync(cacheKey, () => GetTemplateByNameFromDb(templateName, customerId), TimeSpan.FromMinutes(base._cacheDurationInMinutes), _logger);
            return templateData ?? throw new KeyNotFoundException($"template  with name  {templateName} not found.");
        }

        public async Task<Tuple<string, string>> GetTemplateByNameFromDb(string templateName, int? customerId = null)
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
                                    TemplateName='{templateName}';
                                    and OrganizerId<=>{(customerId.HasValue ? customerId.Value : "NULL")}";
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
    }
}