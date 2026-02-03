using EventManagementDbAccess;
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

        public EmailCampaignController(ILogger<EmailCampaignController> logger, EmailCampaignDbAccess dbAccess)
        {
            _logger = logger;
            _campaignDbAccess = dbAccess;
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

      }
}