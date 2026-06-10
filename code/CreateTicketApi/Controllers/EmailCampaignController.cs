using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    public class TemplateContentRequest
    {
        public int TemplateId { get; set; }
        //public string TemplateContent { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("strict-ip-auth-organizer")]
    public class EmailCampaignController : ControllerBase
    {
        private readonly ILogger<EmailCampaignController> _logger;
        private readonly EmailCampaignDbAccess _campaignDbAccess;
        private readonly NotificationTemplateAccess _templateAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDBAccess;
        private readonly EventDbAccess _eventDbAccess;

        private readonly IConfiguration _configuration;
        private readonly SalesOrderDbAccess _salesOrderDbAccess;

        

        public EmailCampaignController(ILogger<EmailCampaignController> logger, EmailCampaignDbAccess dbAccess,
         NotificationTemplateAccess templateAccess, EventOrganizerDBAccess eventOrganizerDBAccess,
          EventDbAccess eventDbAccess, SalesOrderDbAccess salesOrderDbAccess,IConfiguration configuration)
        {
            _logger = logger;
            _campaignDbAccess = dbAccess;
            _templateAccess = templateAccess;
            _eventOrganizerDBAccess =  eventOrganizerDBAccess;
            _salesOrderDbAccess = salesOrderDbAccess;
            _eventDbAccess = eventDbAccess;
            _configuration = configuration;
        }

        [HttpGet("{customerId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> GetByOrganizerId(int customerId)
        {
            if (customerId <= 0)
            {
                return StatusCode(500, "Invalid customer ID");
            }
             _logger.LogInformation($"GetByOrganizerId called for customer id {customerId}");
           
            try
            {
               List<EmailCampaign> retVal = await  _campaignDbAccess.GetEmailCampaignsByOrganizerId(customerId);
               return  Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in retrieving campaigns by org id, {ex}"    );
                return StatusCode(500, "Exception in retrieving campaings by organizer ID");
            }

        }
        [HttpPost("Resolve/{eventId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "EventOwnedByCustomer")]
        public async Task<IActionResult> ResolveReminderTemplate(int eventId, [FromBody] TemplateContentRequest request)
        {
            // if (templateId == 0)
            // {
            //     return StatusCode(500, "Invalid template ID");
            // }   
            if (eventId == 0)
            {
                return StatusCode(500, "Invalid event ID");
            }
            if (request == null || request?.TemplateId == 0)
            {
                return StatusCode(500, "Template ID is required for resolving template");
            }

            try
            {
                
               EventHeader evt = await _eventDbAccess.GetEventHeaderById(eventId);
               if (evt == null)                {
                    return StatusCode(500, "Unable to find event for given event ID");
                }
                _logger.LogInformation($"Event details for id {eventId} are name {evt.EventName} date {evt.EventDate} location {evt.EventLocation} organizer id {evt.EventOrganizerId}");
                EventOrganizer organizer = await _eventOrganizerDBAccess.GetOrganizerById(evt.EventOrganizerId);
                if (organizer == null)
                {
                    return StatusCode(500, "Unable to find organizer for given event");
                }
                var retData= await _templateAccess.GetTemplateById(request.TemplateId);
                OrderEmailDetails orderEmailData = await  _salesOrderDbAccess.GetSampleOrderEmailDetails(eventId);
                string resolvedContent = GetEmailContentToSend(retData.Item1, evt, organizer, orderEmailData);
                return Ok(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(resolvedContent)));
             
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in resolving template by id, {ex}"    );
                return StatusCode(500, $"Exception in resolving template for event ID {eventId}");
            }
          
        }

        private string GetEmailContentToSend(string emailTemplate, EventHeader eventHeader, EventOrganizer eventOrganizer,
                OrderEmailDetails orderEmailData)
        {
              string eventDate = string.Empty,eventTime =string.Empty;
            if (eventHeader.Latitude!=0 && eventHeader.Longitude!=0)
            {
                (eventDate, eventTime)= EventUtils.TimeZoneConverter.GetLocalDateTime((double)eventHeader.Latitude,(double) eventHeader.Longitude,eventHeader.EventDate);
            }
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = orderEmailData?.FullName ?? "Attendee",
                EventLocalDate = eventDate,
                EventLocalTime = eventTime,
                EventLocation = eventHeader.EventLocation,
                EventName = eventHeader.EventName,
                EventOrganizerEmail = eventOrganizer.OrganizerEmail,
                EventOrganizerName = eventOrganizer.OrganizationName,
                QRCode = string.IsNullOrWhiteSpace(orderEmailData?.SalesOrderCode) ? "ABCDEFGH" : orderEmailData.SalesOrderCode,
                GrandTotal = orderEmailData?.SalesOrderTotal >0 ? orderEmailData.SalesOrderTotal.ToString("C") : "$100.00",
                VenueName= " ",
                EventTicketLink = $"{_configuration["BaseUrl"]}/ticketdetails/{EncryptionHelper.Encrypt(orderEmailData.SalesOrderId.ToString(),
                                    _configuration["Encryption:Secretkey"] ?? string.Empty)
                                    }"

            });
            
            var tokenReplacer = new EmailTokenReplacement(_configuration);
            return tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailTemplate)), values);        
        }

        [HttpDelete("{customerId}/{campaignId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> Delete(int customerId, int campaignId)
        {
            if (campaignId <=0 || customerId <=0)
            {
                return StatusCode(500, "Invalid campaign ID or customer ID");
            }
            _logger.LogInformation($"Delete campaign called for campaign id {campaignId} and customer id {customerId}");
        
            try
            {
               EmailCampaign campaign =  await _campaignDbAccess.GetEmailCampaignByCampaignId(campaignId);
               if (campaign == null)
                {
                    return StatusCode(500, "Failed to retrieve campaign to delete");
                }
                bool result = false;
                result =await _campaignDbAccess.DeleteEmailCampaign(campaignId);
                if (!result)
                {
                    return StatusCode(500, "Unable to delete campaign");   
                }
                if (!(await _templateAccess.GetDefaultTemplatesIds()).Contains(campaign.TemplateId))
                {
                    _logger.LogInformation($"Delete template as well since it is not default");
                    await _templateAccess.DeleteTemplate(campaign.TemplateId);
                    if (!result)
                    {
                      return StatusCode(500, "Unable to delete associated template");   
                    }
                }
              
              return  Ok();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in deleting campaign by  id, {ex}");
                return StatusCode(500, $"Exception in campaignId {ex}");
            }

        }


        [HttpGet("byId/{customerId}/{campaignId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]

        public async Task<IActionResult> GetByCampaignId(int customerId,int  campaignId)
        {
            if (campaignId <=0 || customerId <=0)
            {
                return StatusCode(500, "Invalid campaign ID or customer ID");
            }
            try
            {
               EmailCampaign retVal = await  _campaignDbAccess.GetEmailCampaignByCampaignId(campaignId);
               return  Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in retrieving campaigns by org id, {ex}"    );
                return StatusCode(500, "Exception in retrieving campaings by organizer ID");
            }

        }

        [HttpPost("addupdatecampaign/{customerId}/{campaignId}")]
        [Authorize(Policy = "RestrictedAdminMinimum")]
        [Authorize(Policy = "MatchingCustomer")]
        public async Task<IActionResult> Post(int customerId, int campaignId,[FromBody]EmailCampaignTemplate emailCampaign)
        {
            try
            {
                if (customerId <=0)
                {
                    return StatusCode(500, "Invalid customer ID");
                }
                if (emailCampaign == null || emailCampaign.EventId <=0 )
                {
                    return StatusCode(500, "Invalid campaign data. Please provide all required fields");
                }
              
                //creating a new campaign
                if (campaignId <= 0)
                {
                    _logger.LogInformation($"No campaign id found. Processing new campaign.");
                    

                    //new template as well. Should always be true for a new campaign
                    //unless at some point we add clone functionality for existing reminder templates
                    if (emailCampaign.TemplateContentChange)
                    {
                        string templateName = string.Format("{0}_{1}",emailCampaign.EventId,emailCampaign.EmailCampaignName);
                        int templateId = await _templateAccess.AddEmailTemplate(new EmailTemplate()
                        {
                            Subject = emailCampaign.Subject,
                            TemplateContent = emailCampaign.TemplateContent,
                            TemplateName =  templateName,
                            IsDefault= false,
                            TemplateDescription=emailCampaign.Description
                        });
                        if (templateId ==0)
                        {
                            throw new Exception("Unable to create email template for campaign.Aborting process.");
                        }
                        int createdID =await _campaignDbAccess.CreateEmailCampaign(new EmailCampaign()
                        {
                           EventId = emailCampaign.EventId,
                           Name = emailCampaign.EmailCampaignName,
                           Description =emailCampaign.Description,
                           SendAt = emailCampaign.SendNow? DateTime.UtcNow.AddMinutes(1):emailCampaign.SendAt,
                           Status ="Pending",
                           TemplateId = templateId,
                           EventName = emailCampaign.EventName,
                           TemplateName = templateName,
                           TemplateContent=emailCampaign.TemplateContent,
                           TemplateDescription = emailCampaign.Description,
                           IsDefault= false,
                           Subject = emailCampaign.Subject
                        });
                        if (createdID ==0)
                        {
                            throw new Exception("Unable to create email campaign for new campaign.Aborting process.");
                        }
                        _logger.LogInformation($"Created new campaign with id {createdID}");
                        return Ok(createdID);
                    }
                    else
                    {
                        _logger.LogInformation("No template change for new campaign. not prcessing");
                    }
                }
                else
                {
                    _logger.LogInformation($"Campaingn id exists {campaignId}. Processing as update...");
                    if (emailCampaign.TemplateId <=0)
                        return StatusCode(500,$"Template is invalid");
                    //Modifying default template. Need to create new template.
                    if (emailCampaign.TemplateContentChange)                    
                    {
                        int templateId =emailCampaign.TemplateId;
                        var defaultIds= await _templateAccess.GetDefaultTemplatesIds();
                        _logger.LogInformation($"default ids for templates are {string.Join(", ",defaultIds)}");   
                        if (defaultIds.Contains(emailCampaign.TemplateId))
                        {
                            return StatusCode(500,$"Default template cannot be modified. Please choose a different template or create a new campaign with changes to template");
                        }
                        else
                        {
                            //this updates the template in the db and the template cache as well.
                            await _templateAccess.UpdateEmailTemplate(new EmailTemplate()
                            {
                                Id=templateId,
                                Subject = emailCampaign.Subject,
                                TemplateContent = emailCampaign.TemplateContent,
                                TemplateName = string.Format("{0}_{1}",emailCampaign.EventId,emailCampaign.EmailCampaignName),
                                IsDefault= false,
                                TemplateDescription=emailCampaign.Description,
                                
                            });
                        }
                        //this updates the campaign info in db and the campaign cache as well.
                        //the template details being sent here like content and description are not being used
                        //  for update but just for cache update. 
                        // So even if there is no change in template we are sending the details
                        //  to ensure cache has the latest info.
                        //So essentially the template details are cached in 2 places - one in template cache and another in campaign cache. This is to ensure we have the details available in campaign cache for sending email without having to call template cache separately.
                        //This is odd but I do not know of a better way to ensure we have all the details in campaign cache for email sending without having to call template cache separately.
                        bool result  = await _campaignDbAccess.UpdateEmailCampaign(new EmailCampaign()
                        {
                                Id =  campaignId,
                                Name = emailCampaign.EmailCampaignName,
                                Description = emailCampaign.Description,
                                SendAt = emailCampaign.SendNow? DateTime.UtcNow : emailCampaign.SendAt,
                                TemplateId = templateId,
                                Status ="Pending",
                                EventId = emailCampaign.EventId,
                                TemplateContent=emailCampaign.TemplateContent,
                                TemplateDescription = emailCampaign.Description,
                                Subject = emailCampaign.Subject,
                                IsDefault= false,
                        });
                        
                        if (!result)
                        {
                            return StatusCode(500,"Error updating email campaign");
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                
                _logger.LogCritical($"error in adding or updating campaignsfor id {campaignId}, {ex}"    );
                return StatusCode(500, $"Exception in processing email campaign {ex}");
            }
        }

      }
}