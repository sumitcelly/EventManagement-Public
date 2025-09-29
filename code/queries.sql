INSERT INTO `eventmanagement`.`attendee`
(
`Email`

)
VALUES
(
'scelly1@securevideo.com');

select * from `eventmanagement`.`salesorder`

select * from `eventmanagement`.`salesorder`

delete from `eventmanagement`.`eventsalesitem` where salesorderid is  null

SELECT UUID();

SELECT @@global.time_zone, @@session.time_zone;

SELECT a.Name, a.Email, a.Sms, c.Description,
                                  b.CreatedAt, b.ModifiedAt, 
                                  b.TicketCode, b.TicketScanned 
                                  from eventmanagement.Attendee a, 
                                  eventmanagement.EventSalesItem b,
                                  eventmanagement.EventItemType c
                                 where a.attendeeid=b.attendeeid
                                   AND b.EventItemTypeId = c.EventItemTypeId
                                   And b.EventId = c.EventId
                                   AND b.SalesOrderId = 1
                                   AND b.EventId = 1
                                   
SELECT a.OrderId, b.TicketCode,b.TicketScanned, c.EventItemTypeId,c.Name 
                                    from SalesOrder a, EventSalesItem b, EventItemType c
                                    where a.OrderId=b.SalesOrderId and
                                    b.EventItemTypeId=c.EventItemTypeId and
                                    a.SalesOrderCode='MEQL0EOF' and 
                                    a.eventId=1