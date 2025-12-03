using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using EventUtils;
using Amazon.S3.Model;
using static EventUtils.AmazonS3ContentUploader;
using Stripe;
using EventManagementDbAccess; // Adjust namespace if AmazonS3ContentUploader is elsewhere

namespace CreateTicketApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;
        private readonly AmazonS3ContentUploader _s3Uploader;
        private readonly string _contentPath = string.Empty;

        private readonly EventOrganizerDBAccess _evtOrganizerDbAccess;

        private readonly EventDbAccess _evtDbAccess;

        public FileUploadController(IWebHostEnvironment env, ILogger<FileUploadController> logger, 
                                AmazonS3ContentUploader s3Uploader,EventOrganizerDBAccess evtOrganizerDbAccess,
                                EventDbAccess evtDbAccess)
        {
            _logger = logger;
            _s3Uploader = s3Uploader;
            _contentPath = env.ContentRootPath+"\\Content\\Customer\\";
            _evtOrganizerDbAccess = evtOrganizerDbAccess;
            _evtDbAccess = evtDbAccess;
        }

        [HttpPost("uploaddev")]
        /// <summary>
        /// Uploads a file to the local file system for development purposes.
        public async Task<IActionResult> UploadFileDev(IFormFile file, int organizationId, int eventId, string purpose)
        {
            if (file == null || file.Length == 0 || organizationId <=0 )
                return BadRequest("File, customerName, and eventName are required.");

            try
            {
                if (!Enum.TryParse(purpose, out Purpose filePurpose))
                    return BadRequest("A valid file purpose is required.");
                string filePath = GetFileKey(file.FileName, organizationId,filePurpose, eventId);
                filePath = filePath.Replace('/', '\\'); // Ensure correct path format for Windows
                filePath = Path.Combine(_contentPath, filePath);
                if (file.Length > 0 && file.Length < 10 * 1024 * 1024) // Limit to 10MB
                {
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else
                {
                    _logger.LogWarning("File size exceeds the limit or is empty.");
                    return BadRequest("File size exceeds the limit or is empty.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file.");
                return StatusCode(500, "Failed to upload file.");
            }
            return Ok("File uploaded successfully.");
        }


        /// <summary>
        /// Uploads a file to Amazon S3 but really not used in production.
        /// This is just a test method to check if the file upload works.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="customerName"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        // [HttpPost("uploadtest")]
        // public async Task<IActionResult> UploadFileTest(IFormFile file, string customerName,  string eventName)
        // {
        //     if (file == null || file.Length == 0 || string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(eventName))
        //         return BadRequest("File, customerName, and eventName are required.");

        //     try
        //     {
        //         await _s3Uploader.UploadFileAsync(customerName, eventName, file.OpenReadStream(), file.FileName, file.ContentType);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Failed to upload file to S3.");
        //         return StatusCode(500, "Failed to upload file to S3.");
        //     }
        //     return Ok("File uploaded successfully.");
        // }

        /// <summary>
        /// The presigned URL is used to upload a file to Amazon S3. The url returned should be used by JS to send a put request
        /// as indictated in this code C:\temp\projects\youtube-samples\s3-presigned-urls\Index - Generate PreSigned.html
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="customerName"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>

        [HttpGet("presigned-url")]
        public async Task<IActionResult> GetPresignedUrl([FromQuery] string fileName, [FromQuery] int organizationId,
        [FromQuery] string contentType,
         [FromQuery] int eventId,[FromQuery] string filePurpose)
        {
            if (string.IsNullOrWhiteSpace(fileName) || organizationId <=0 )
            return BadRequest("File, customerName, and eventName are required.");
       

            if (!Enum.TryParse(filePurpose, out Purpose purpose))
                return BadRequest("A valid file purpose is required.");
            try
            {
                bool result = false;
                if (purpose == Purpose.OrganizerAboutMeImage)
                {
                    result =await _evtOrganizerDbAccess.UpdateOrganizerImageUrl(organizationId,
                    AmazonS3ContentUploader.GetFileKey(fileName,organizationId,purpose));
                }
                if (purpose == Purpose.EventBannerImage)
                {
                    result = await _evtDbAccess.UpdateEventBannerImageUrl(organizationId,
                    AmazonS3ContentUploader.GetFileKey(fileName,organizationId,purpose,eventId));
                }
                if (!result)
                {
                    return StatusCode(500, "Failed to generate presigned URL due to DB issues");
                }
                var url = await _s3Uploader.GetPreSignedUrlForUpload(fileName, organizationId, purpose, contentType,eventId);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate presigned URL.");
                return StatusCode(500, "Failed to generate presigned URL.");
            }
        }

    }
}