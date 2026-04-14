select * from logincodes order by createdat desc
delete from eventorganizermembers where customerid=42
delete from eventorganizer where customerid=42

select * from notificationtemplates order by modifiedat desc

update notificationtemplates as t1
JOIN notificationtemplates AS t2 ON t1.matching_id = t2.source_id
set templatename='EventReminder7day',
subject='Your event is coming up in 7 days',

templatecontent= (select templatecontent from notificationtemplates where id=33)
where id=4

UPDATE notificationtemplates
SET templatename = (
    SELECT templatename 
    FROM (SELECT * FROM notificationtemplates) AS temp_alias 
    WHERE id = 33),
    templatename='EventReminder7day',
	subject='Your event is coming up in 7 days',
    templatedescription='7 day event reminder'
WHERE id = 4;

select * from emailcampaign order by createdat desc
update emailcampaign
set status='Pending'
where id=16

update notificationtemplates
set TemplateName='OrderConfirmation',
TemplateDescription='Order confirmation template'
where id=3

if not exists (select count(*) from emailcampaign where eventid=37)
update emailcampaign
set status='Pending'
where eventid=37

delete from emailcampaign
where id>0
select * from emailrecipients order by createdat desc

SELECT * FROM emailrecipients WHERE emailcampaignid = 16 and status != 'Queued' and  (retrycount is null or retrycount < 3)

delete from emailrecipients
where id>0

select * from emailtransactionlog
select * from events where eventname='Drinks Festival NY'
update events
set islive=1
where eventid=6
select * from logincodes order by createdat desc

select * from eventuser where email like 'test77%'
update events
set islive=1
where eventid>0
select * from eventsalesitem where ticketcode='XF6OJJ1I'

SELECT 
                        CustomerId, 
                        OrganizationName,
                        OrganizerEmail,
                        OrganizerWebsite,
                        OrganizerEventBaseUrl,
                        OrganizerDescription,
                        OrganizerAboutMe,
                        OrganizerImageUrl,
                        OrganizerCity,
                        OrganizerCountry,
                        OrganizerPhone,
                        OrganizerStreetAddress,
                        OrganizerZipCode,
                        OrganizerInstagram,
                        OrganizerFacebook,
                        OrganizerX,
                        StripeAccountId,
                        StripeConnectStatus                      
                    FROM eventorganizer 
                    WHERE CustomerId = 1
select * from eventorganizer where customerid=40

select  eventId, eventname, eventurlname from events order by createdat desc

select * from events where eventid =32
update events
set eventurlname='drinksfestivalny'
where eventid=6

update eventorganizer
set OrganizerEventBaseUrl='pdac25'
where customerid=1

select * from eventitemtype where eventitemtypeid=15
update eventitemtype
set cost =1.67
where eventitemtypeid=15