# select * from notificationtemplates  -4 is id

INSERT INTO `eventmanagement`.`emailcampaign`
(
`TemplateId`,
`EventId`,
`SendAt`,
`Status`,

)
VALUES
(
4,
6,
current_timestamp,
"Pending",
);
