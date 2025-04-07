using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;


using Org.BouncyCastle.Tls.Crypto.Impl.BC;
namespace EventDbAccess
{
    public class TicketAccess
    {
        
        private readonly string ConnectionString;
        public TicketAccess(string connectionString)
        {
            this.ConnectionString = connectionString;
            
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