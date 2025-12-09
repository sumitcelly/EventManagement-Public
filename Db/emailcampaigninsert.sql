# select * from notificationtemplates  -4 is id

INSERT INTO `eventmanagement`.`emailcampaign`
(
`TemplateId`,
`EventId`,
`SendAt`,
`Status`
)
VALUES
(
4,
6,
current_timestamp,
"Pending"
);
SELECT * FROM emailcampaign  -- WHERE Status = 'Pending' and  SendAt <= Utc_timestamp()
select * from emailrecipients

delete from emailrecipients
where id>0

update emailcampaign
set Status='Pending'
where id>0

update salesorder
set email='testuser1@gmail.com'
where orderid=88


SELECT a.Email, a.FullName, b.OrderId FROM eventuser a, salesorder b 
                                WHERE b.EventId = 6 and 
                                b.UserId = a.UserId