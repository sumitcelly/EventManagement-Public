
Business registeration:
* need company name, domain, product name.
* Need to register.
*

UI:
* Footer with all the links.
* Help etc.
* Bugs in ticket setup
* Mapbox issues with double click.
* event page looks ugly.
* sanitize inputs for event and ticket pages or whereever html is allowed.

Whats left in attendee:
* Legal agreement on tickets page.
* Payment integration.

Whats left in Organizer:
* Legal agreements when signing up.
* Email marketing to imported list of users.
* How does someone become an organizer in the system? Whats the flow for that?

Mobile:
* iphone app
* finalize and package apps.

Architectural Stuff around whats left:
* Payment is another one.
    * Stripe UI for attendees to pay. Refunds and how they work.
    * Stripe setup for Organizers.
* Role validation at backend and also front end.
* Logging  to disk at least
* Deployment
    * Need to revisit plan and acquire resources.
    * ec2/container, DB, cloudfront pointing to s3 etc.
    * pipelines.




