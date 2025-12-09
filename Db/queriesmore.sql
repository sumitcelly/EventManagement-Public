
select * from events where eventid=28
select * from eventorganizer where customerid=1

SELECT a.UserId,a.Role,a.IsActive, b.email,b.fullname
 FROM eventorganizermembers a, eventuser b WHERE
 a.userid=b.userid and
 CustomerId = 1
 
 SELECT a.OrderId, a.SalesOrderCode, a.SalesOrderStatus,a.CreatedAt,
                                b.EventName, c.Email, c.FullName,
                                sum(d.cost)
                                from SalesOrder a, Events b, EventUser c, eventitemtype d, EventSalesItem e
                                where a.EventId = b.EventId and a.UserId = c.UserId and
                                d.eventitemtypeid= e.eventitemtypeid and
                                e.salesorderid = a.orderid
                                and a.customerId = 1 
                                group by
                                a.OrderId,a.SalesOrderCode, a.SalesOrderStatus,
                                a.CreatedAt, b.EventName, c.Email, c.FullName
                                
SELECT 
    a.OrderId,
    a.SalesOrderCode,
    a.SalesOrderStatus,
    a.CreatedAt,
    b.EventName,
    c.Email,
    c.FullName,
    COALESCE(SUM(d.cost), 0) AS OrderTotal
FROM SalesOrder a
JOIN Events b ON a.EventId = b.EventId
JOIN EventUser c ON a.UserId = c.UserId
LEFT JOIN EventSalesItem e ON e.salesorderid = a.orderid
LEFT JOIN eventitemtype d ON d.eventitemtypeid = e.eventitemtypeid
WHERE a.customerId = 1
GROUP BY
    a.OrderId,
    a.SalesOrderCode,
    a.SalesOrderStatus,
    a.CreatedAt,
    b.EventName,
    c.Email,
    c.FullName
ORDER BY a.CreatedAt DESC;
 
 select sum(b.cost) from eventsalesitem a, eventitemtype b
 where a.salesorderid=86 and a.eventitemtypeid = b.eventitemtypeid
 
 select a.EventItemTypeId, b.description, b.cost from eventsalesitem a, eventitemtype b 
 where a.salesorderid=75 and a.eventitemtypeid = b.eventitemtypeid
 
  
 update eventuser
 set createdat=utc_timestamp()
 where userid>0
 ALTER TABLE `eventmanagement`.`eventuser` 
CHANGE COLUMN `ModifiedAt` `ModifiedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;