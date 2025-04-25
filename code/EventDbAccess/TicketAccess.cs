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
                throw new ArgumentNullException("code");
            }
            bool retVal= false;
            try
            {    
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$" Update eventmanagement.eventticket set TicketScanned=1  where
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

        public async Task<EventTicket> GetEventTicketByQRCode(string code, int eventId =1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code");
            }

            EventTicket ticket = new EventTicket();
            try
            {    
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$"Select AttendeeName, AttendeeEmail, AttendeeSms,TicketScanned from eventmanagement.eventticket where
                                    EventId='{eventId}' and TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.RecordsAffected >1)
                            throw new Exception("More than one record returned for ticket code"+code);
                      
                        while (await reader.ReadAsync())
                        {
                            ticket.AttendeeName = reader.GetString(0);
                            ticket.AttendeeEmail = reader.GetString(1);
                            ticket.AttendeeSms = reader.GetString(2);
                            ticket.TicketScanned = reader.GetInt16(3);
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

        public bool AddEventTicket(EventTicket ticket)
        {
            try
            {
                using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnectionString))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO eventmanagement.eventticket (EventId,AttendeeName,AttendeeEmail,AttendeeSms,TicketScanned,TicketCode)");
                    sb.Append(" VALUES (");
            
                    sb.Append(ticket.EventId);
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.AttendeeName);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.AttendeeEmail);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.AttendeeSms);
                    sb.Append("'");
                    sb.Append(",");
                    sb.Append(ticket.TicketScanned);
                    sb.Append(",");
                    sb.Append("'");
                    sb.Append(ticket.TicketCode);
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