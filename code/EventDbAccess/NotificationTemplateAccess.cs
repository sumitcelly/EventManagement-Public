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
namespace EventDbAccess
{
    public class NotificationTemplateAccess :BaseDbAccess
    {
        

        public NotificationTemplateAccess(IConfiguration connectionString, ILogger<NotificationTemplateAccess> logger) :base (connectionString, logger) 
        {
          
            
        }

        public async Task<string> GetTemplateByName(string templateName, int customerId=1)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                throw new ArgumentNullException("templateName");
            }
        
            string templateContent = string.Empty;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$"Select TemplateContent from eventmanagement.notificationtemplates where
                                    TemplateName='{templateName}' and OrganizerId='{customerId}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.RecordsAffected > 1)
                            throw new Exception("More than one record returned for template Name" + templateName);

                        while (await reader.ReadAsync())
                        {
                            templateContent = reader.GetString(0);
                           
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return templateContent;
        }

       
    }
}