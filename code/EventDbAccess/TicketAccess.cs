using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

using System.Data.Common;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
namespace EventManagementDbAccess
{

    public class TicketAccess :BaseDbAccess
    {
        public TicketAccess(IConfiguration config, ILogger<TicketAccess> logger) :base(config, logger)
        {
            
        }
        public async Task<bool> ValidateTicket(string code, int eventId = 1)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }
            bool retVal = false;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @$" Update eventmanagement.eventsalesitem set TicketScanned=1  where
                                EventId='{eventId}' and TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    int val = await cmd.ExecuteNonQueryAsync();
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

        public async Task<EventSalesItem> GetEventTicketByQRCode(string code, int eventId = 1)
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
                                b.TicketCode, b.TicketScanned 
                                from eventmanagement.Attendee a, 
                                eventmanagement.EventSalesItem b where
                                a.AttendeeId=b.AttendeeId and
                                b.EventId='{eventId}' and b.TicketCode='{code}'";
                    await connection.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.RecordsAffected > 1)
                            throw new Exception("More than one record returned for ticket code" + code);

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

        public async Task<int> AddEventTicket(EventSalesItem ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            try
            {
                using MySqlConnection mySqlConnection = new MySqlConnection(this.ConnectionString);

                StringBuilder sb = new StringBuilder();
                sb.Append(@"INSERT INTO eventmanagement.eventsalesitem (EventId,AttendeeId,
                        TicketScanned,TicketCode,SalesOrderId,EventItemTypeId,
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
                sb.Append(ticket.EventItemType.EventItemTypeId);
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
                int i = await cmd.ExecuteNonQueryAsync();
                if (i == 1)
                {
                    return cmd.LastInsertedId > 0 ? Convert.ToInt32(cmd.LastInsertedId) : 0;
                }
                else
                {
                    throw new Exception("Failed to create ticket.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<EventSalesItem>> GetEventTicketBySalesOrderId(int salesOrderId, int eventId)
        {
            if (salesOrderId <= 0 || eventId <= 0)
                throw new ArgumentException("SalesOrderId and EventId must be greater than zero.");

            List<EventSalesItem> ticketList = new List<EventSalesItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectionString))
                {
                    string sql = @"SELECT a.Name, a.Email, a.Sms, c.Description,
                                c.EventItemTypeId,c.Name as ItemName, c.Cost,
                                b.CreatedAt, b.ModifiedAt, 
                                b.TicketCode, b.TicketScanned 
                                from eventmanagement.Attendee a, 
                                eventmanagement.EventSalesItem b,
                                eventmanagement.EventItemType c
                                where a.attendeeid=b.attendeeid
                                AND b.EventItemTypeId = c.EventItemTypeId
                                And b.EventId = c.EventId
                                AND b.SalesOrderId = @salesOrderId 
                                AND b.EventId = @eventId";
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@salesOrderId", salesOrderId);
                    cmd.Parameters.AddWithValue("@eventId", eventId);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        _logger.LogInformation($"Records affected: {reader.RecordsAffected}");
                            while (await reader.ReadAsync())
                        {
                            EventSalesItem ticket = new EventSalesItem();
                            ticket.Attendee = new Attendee()
                            {
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Sms = reader.GetString(reader.GetOrdinal("Sms"))
                            };

                            ticket.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            ticket.ModifiedAt = reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));
                            ticket.TicketCode = reader.GetString(reader.GetOrdinal("TicketCode"));
                            ticket.TicketScanned = reader.GetInt32(reader.GetOrdinal("TicketScanned"));
                            ticket.EventItemType = new EventItemType()
                            {
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                                EventItemTypeId = reader.GetInt32(reader.GetOrdinal("EventItemTypeId")),
                                Name = reader.GetString(reader.GetOrdinal("ItemName"))
                            };
                            ticketList.Add(ticket);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return ticketList;
        }
    }
}