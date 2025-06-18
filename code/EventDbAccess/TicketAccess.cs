using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;


using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using System.Threading.Tasks;
using System.Data.Common;
using System.Security;
namespace EventDbAccess
{
    public class TicketAccess
    {
        
        private readonly string ConnectionString;
        public TicketAccess(string connectionString)
        {
            this.ConnectionString = connectionString;
            
        }

        public async Task<bool> ValidateTicket(string code, int eventId=1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }
            bool retVal= false;
            try
            {    
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$" Update eventmanagement.eventsalesitem set TicketScanned=1  where
                                    EventId='{eventId}' and TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    int val =await cmd.ExecuteNonQueryAsync() ;
                    Console.WriteLine($"Records update for {code} is {val}");
                    retVal = val == 1 ? true : false;
                }
              
            }
            catch (Exception ex)   
            {
                Console.WriteLine(ex.Message);
            }
            return retVal;
        }

        public async Task<EventSalesItem> GetEventTicketByQRCode(string code, int eventId =1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code");
            }

            //Todo: Need a UI model here to return data for attendee plus ticket
            EventSalesItem ticket = new EventSalesItem();
            try
            {    
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$"Select a.Name, a.Email, a.Sms, 
                                    b.CreatedAt, b.ModifiedAt, 
                                    b.TicketCode b.TicketScanned 
                                    from eventmanagement.Attendee a, 
                                    eventmanagement.EventSalesItem b where
                                    a.AttendeeId=b.AttendeeId and
                                    b.EventId='{eventId}' and b.TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.RecordsAffected >1)
                            throw new Exception("More than one record returned for ticket code"+code);

                        while (await reader.ReadAsync())
                        {
                            ticket.Attendee = new Attendee()
                            {
                                Name = reader.GetString(0),
                                Email = reader.GetString(1),
                                Sms = reader.GetString(2)
                            };

                            ticket.CreatedAt = reader.GetDateTime(3);
                            ticket.ModifiedAt = reader.GetDateTime(4);
                            ticket.TicketCode = reader.GetString(5);
                            ticket.TicketScanned = reader.GetInt32(6);
                            
                        }
                    }
                }
              
            }
            catch (Exception ex)   
            {
                Console.WriteLine(ex.Message);
            }
            return ticket; 
        }

        public bool AddEventTicket(EventSalesItem ticket)
        {
            try
            {
                using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(@"INSERT INTO eventmanagement.eventsalesitem (EventId,AttendeeId
                                TicketScanned,TicketCode,SalesOrderId,TicketTypeId,
                                CreatedAt,ModifiedAt) ");
                    sb.Append(" VALUES (");
            
                    sb.Append(ticket.EventId);
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.Attendee.AttendeeId);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append(ticket.TicketScanned);
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.TicketCode);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.SalesOrderId);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.TicketTypeId);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));    
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");     
                    sb.Append(ticket.ModifiedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.Append("'");
                    sb.Append(")");
                    
                    Console.WriteLine(sb.ToString());
                    mySqlConnection.Open();
                    MySqlCommand cmd = new MySqlCommand(sb.ToString(), mySqlConnection);
                    int i =cmd.ExecuteNonQuery();
                    return i ==1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}