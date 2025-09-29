SELECT * FROM eventmanagement.events;
SELECT * FROM eventmanagement.events WHERE MATCH(EventDescription) AGAINST(`food` IN NATURAL LANGUAGE MODE);




select  a.EventId,a.EventName,a.EventHeadline,a.EventDate, a.EventOrganizer, a.EventSummary,a.Free,ifnull(a.EventAddress,'') as EventAddress, b.OrganizerName from 
`eventmanagement`.`events` a,
`eventmanagement`.`eventorganizer` b
where a.EventOrganizer= b.CustomerId 

select a.EventId,a.EventName,a.EventHeadline,a.EventDate, a.EventOrganizer, a.EventSummary,a.Free,a.eventAddress,
                    b.OrganizerName from events a, eventorganizer b 
                    WHERE  a.EventOrganizer= b.CustomerId and 
                    a.EventId = 1

update  events
set City="Colorado Springs"
where eventid>0



INSERT INTO `eventmanagement`.`events`
(
`EventName`,
`EventDate`,
`EventOrganizer`,
`EventDescription`,
`EventAddress`,
`State`,
`City`,
`EventCategory`,
`EventTags`,
`EventDuration`,
`EventScheduleType`,
`EventHeadline`,
`EventAgenda`,
`Private`,
`ImageReel`)
VALUES
(
'Music Festival NY',
'2026-09-29 11:00:00',
1,
'Many vendors, food stall, cultural, live music, fun, dances, culture, and much much more',
'123 Lewis Palmer School, Colorado Springs, CO - 80920',
'Colorado Springs',
'CO',
'Food Festivals',
'Food, Outdoors, Live music',
4,
'OneTime',
'Come enjoy the food, outdoors, music',
'',
0,
0);
SELECT * FROM eventmanagement.events;

SELECT * FROM eventmanagement.events WHERE MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventCategory,EventName) 
AGAINST ('festival' IN NATURAL LANGUAGE MODE);

SELECT 
        EventId,
        City,
        EventDate,
        eventheadline,
        EventDescription,
        EventTags,
        EventDate,
        EventAddress,
        EventCategory 
		FROM events
        WHERE
        city='Colorado Springs'
        and eventdate >'9/29/2026 12:00:00 AM'
        order by EventDate asc
   
   MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) 
    
CALL search_events(
    NULL,                   -- p_keyword
    NULL,                 -- p_start_date
    NULL, 					-- p_end_date
    NULL,                  -- p_state
   'Colorado Springs',			-- p_city
    NULL,                      -- p_category
    10,                        -- p_limit
    0                          -- p_offset
);