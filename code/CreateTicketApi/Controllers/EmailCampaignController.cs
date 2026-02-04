using EventManagementDbAccess;
using EventUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailCampaignController : ControllerBase
    {
        private readonly ILogger<EmailCampaignController> _logger;
        private readonly EmailCampaignDbAccess _campaignDbAccess;
        private readonly NotificationTemplateAccess _templateAccess;

        public EmailCampaignController(ILogger<EmailCampaignController> logger, EmailCampaignDbAccess dbAccess, NotificationTemplateAccess templateAccess)
        {
            _logger = logger;
            _campaignDbAccess = dbAccess;
            _templateAccess = templateAccess;
        }

        [HttpGet("/byorganizerid/{organizerId}")]
        public async Task<IActionResult> GetByOrgaizerId(int organizerId)
        {
            try
            {
               List<EmailCampaign> retVal = await  _campaignDbAccess.GetEmailCampaignsByOrganizerId(organizerId);
               return  Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in retrieving campaigns by org id, {ex}"    );
                return StatusCode(500, "Exception in retrieving campaings by organizer ID");
            }

        }

        [HttpGet("/bycampaignId/{campaignId}")]
        public async Task<IActionResult> GetByCampaignId(int campaignId)
        {
            try
            {
               EmailCampaign retVal = await  _campaignDbAccess.GetEmailCampaignsByCampaignId(campaignId);
               return  Ok(retVal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"error in retrieving campaigns by org id, {ex}"    );
                return StatusCode(500, "Exception in retrieving campaings by organizer ID");
            }

        }

        [HttpPost("addupdatecampaign/{campaignId}")]
        public async Task<IActionResult> Post(int campaignId,[FromBody]EmailCampaignTemplate emailCampaign)
        {
            try
            {
               
                //creating a new campaign
                if (campaignId <= 0)
                {
                    _logger.LogInformation($"No campaign id found. Processing new campaign.");
                    
                    if (NotificationTemplateAccess._defaultTemplateName.Contains(emailCampaign.EmailTemplateName))
                    {
                        return StatusCode(500,$"Please change name of template since it matches default template");
                    }
                    //new template as well. Should always be true for a new campaign
                    //unless at some point we add clone functionality for existing reminder templates
                    if (emailCampaign.TemplateContentChange)
                    {
                        int templateId = await _templateAccess.AddEmailTemplate(new EmailTemplate()
                        {
                            Subject = emailCampaign.Subject,
                            TemplateContent = emailCampaign.TemplateContent,
                            TemplateName = emailCampaign.EmailTemplateName,
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
                           SendAt = emailCampaign.SendNow? DateTime.UtcNow.AddMinutes(2):emailCampaign.SendAt,
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
                                TemplateName = string.Format("{0}_{1}",emailCampaign.EmailTemplateName,PasswordGenerator.GetPassword()),
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
                            if (NotificationTemplateAccess._defaultTemplateName.Contains(emailCampaign.EmailTemplateName))
                            {
                                return StatusCode(500,$"Please change name of template since it matches default template");
                            }
                            await _templateAccess.UpdateEmailTemplate(new EmailTemplate()
                            {
                                Id=templateId,
                                Subject = emailCampaign.Subject,
                                TemplateContent = emailCampaign.TemplateContent,
                                TemplateName = emailCampaign.EmailTemplateName,
                                IsDefault= false,
                                TemplateDescription=emailCampaign.Description,
                                
                            });
                        }
                        bool result  = await _campaignDbAccess.UpdateEmailCampaign(new EmailCampaign()
                        {
                                Id =  campaignId,
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