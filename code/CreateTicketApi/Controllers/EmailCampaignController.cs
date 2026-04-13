using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public string TemplateContent { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class EmailCampaignController : ControllerBase
    {
        private readonly ILogger<EmailCampaignController> _logger;
        private readonly EmailCampaignDbAccess _campaignDbAccess;
        private readonly NotificationTemplateAccess _templateAccess;

        private readonly EventOrganizerDBAccess _eventOrganizerDBAccess;
        private readonly EventDbAccess _eventDbAccess;

        private readonly SalesOrderDbAccess _salesOrderDbAccess;

        public EmailCampaignController(ILogger<EmailCampaignController> logger, EmailCampaignDbAccess dbAccess,
         NotificationTemplateAccess templateAccess, EventOrganizerDBAccess eventOrganizerDBAccess,
          EventDbAccess eventDbAccess, SalesOrderDbAccess salesOrderDbAccess)
        {
            _logger = logger;
            _campaignDbAccess = dbAccess;
            _templateAccess = templateAccess;
            _eventOrganizerDBAccess =  eventOrganizerDBAccess;
            _salesOrderDbAccess = salesOrderDbAccess;
            _eventDbAccess = eventDbAccess;
        }

        [HttpGet("/EmailCampaign/{customerId}")]
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
        [HttpPost("/EmailCampaign/Resolve/{eventId}")]
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
            if (string.IsNullOrEmpty(request?.TemplateContent))
            {
                return StatusCode(500, "Template content is required for resolving template ID");
            }

            try
            {
                //byte[] fileBytes = System.IO.File.ReadAllBytes(@"C:\temp\projects\eventmgmt\code\CreateTicketApi\Content\EventReminder5day.html");
                //string base64String = Convert.ToBase64String(fileBytes);
               //string rawContent = Encoding.UTF8.GetString(Convert.FromBase64String(request.TemplateContent));
               //string rawContent = base64String;
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
                OrderEmailDetails orderEmailData = await  _salesOrderDbAccess.GetSampleOrderEmailDetails(eventId);
                string resolvedContent = GetEmailContentToSend(request?.TemplateContent, evt, organizer, orderEmailData);
                return Ok(Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(resolvedContent)));
             
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in resolving template by id, {ex}"    );
                return StatusCode(500, $"Exception in resolving template for event ID {eventId}");
            }
          
        }

        private string GetEmailContentToSend(string emailTemplate, EventHeader eventHeader, EventOrganizer eventOrganizer,
                OrderEmailDetails? orderEmailData)
        {
            var values = EmailTokenReplacement.GetReplacementValues(new TokenValues()
            {
                Attendee = orderEmailData?.FullName ?? "Attendee",
                EventDate = eventHeader.EventDate,
                EventLocation = eventHeader.EventLocation,
                EventName = eventHeader.EventName,
                EventOrganizerHelpLine = eventOrganizer.OrganizerPhone,
                EventOrganizerName = eventOrganizer.OrganizationName,
                EventTicketLink = orderEmailData?.SalesOrderId>0?
                                    $"http://localhost:5173/ticketdetails/{WebUtility.UrlEncode(EncryptionHelper.Encrypt(orderEmailData.SalesOrderId.ToString()))}"
                                    :string.Empty
            });
            
            var tokenReplacer = new EmailTokenReplacement();
            return tokenReplacer.ReplaceTokens(
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(emailTemplate)), values);        
        }

        [HttpDelete("/EmailCampaign/{customerId}/{campaignId}")]
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
               
                //creating a new campaign
                if (campaignId <= 0)
                {
                    _logger.LogInformation($"No campaign id found. Processing new campaign.");
                    
                    // if (NotificationTemplateAccess._defaultTemplateName.Contains(emailCampaign.EmailTemplateName))
                    // {
                    //     return StatusCode(500,$"Please change name of template since it matches default template");
                    // }
                    //new template as well. Should always be true for a new campaign
                    //unless at some point we add clone functionality for existing reminder templates
                    if (emailCampaign.TemplateContentChange)
                    {
                        int templateId = await _templateAccess.AddEmailTemplate(new EmailTemplate()
                        {
                            Subject = emailCampaign.Subject,
                            TemplateContent = emailCampaign.TemplateContent,
                            TemplateName = string.Format("{0}_{1}",emailCampaign.EventId,emailCampaign.EmailCampaignName),
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
                           TemplateId = templateId
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
                            _logger.LogInformation("Default template being modifed {0}",emailCampaign.TemplateId);
                            templateId = await _templateAccess.AddEmailTemplate(new EmailTemplate()
                            {
                                Subject = emailCampaign.Subject,
                                TemplateContent = emailCampaign.TemplateContent,
                                TemplateName = string.Format("{0}_{1}",emailCampaign.EventId,emailCampaign.EmailCampaignName),
                                IsDefault= false,
                                TemplateDescription=emailCampaign.Description
                            });
                            if (templateId <=0)
                            {
                                return StatusCode(500,"Error creating template for campaign");
                            }
                        }
                        else
                        {
                            // if (NotificationTemplateAccess._defaultTemplateName.Contains(emailCampaign.EmailTemplateName))
                            // {
                            //     return StatusCode(500,$"Please change name of template since it matches default template");
                            // }
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
                        bool result  = await _campaignDbAccess.UpdateEmailCampaign(new EmailCampaign()
                        {
                                Id =  campaignId,
                                Name = emailCampaign.EmailCampaignName,
                                Description = emailCampaign.Description,
                                SendAt = emailCampaign.SendNow? DateTime.UtcNow : emailCampaign.SendAt,
                                TemplateId = templateId,
                                Status ="Pending",
                                EventId = emailCampaign.EventId
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