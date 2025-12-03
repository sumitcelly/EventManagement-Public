
using System;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace EventUtils;

public class AmazonS3ContentUploader
{
   
    public enum Purpose
    {
        EventBannerImage,
        EventContentImage,
        EventContentDocument,
        OrganizerAboutMeImage,
        OrganizerOtherImage,
        OrganizerDocument 
    } 

    private readonly int _maxTimeForUrl = 30; // in minutes

    private readonly AmazonS3Client _s3Client;

    private  static Microsoft.Extensions.Logging.ILogger? _logger { get; set; }
    public AmazonS3ContentUploader(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger<AmazonS3ContentUploader> logger)
    {
        _logger = logger;

        _s3Client = new AmazonS3Client(
            configuration["AccessKeyId"],
            configuration["AccessKeySecret"],
            Amazon.RegionEndpoint.USWest2);
        if (string.IsNullOrEmpty(BucketName))
        {
            throw new ArgumentException("S3 bucket name is not configured.");
        }
       
    }
    
    public static string BucketName { get; set; } = "customereventcontent";
    public static bool CheckImageFileExtension(string fileName) => 
            fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
    public static bool CheckDocFileExtension(string fileName) => 
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase);
    
    public static string GetFileKey(string fileName, int organizerId,Purpose contentPurpose, int eventId=0)
    {
        // Example key format: "customer event content/CustomerName/Events/EventName/"
        string key = string.Empty;
        if (contentPurpose.ToString().Contains("Image") && !CheckImageFileExtension(fileName))
        {
            _logger?.LogError($"Unable to store image with filename {fileName} because extension is not valid");
            return key;
        }
        if (contentPurpose.ToString().Contains("Document") && !CheckDocFileExtension(fileName))
        {
            _logger?.LogError($"Unable to store document with filename {fileName} because extension is not valid");
            return key;
        }
        if (contentPurpose.ToString().Contains("Event") && eventId==0)
        {
             _logger?.LogError($"For event purpose, eventId must be valid");
            return key;
        }
        switch (contentPurpose)
        {
            case Purpose.EventBannerImage:
                {                
                    key = $"public/{organizerId}/Events/{eventId}/Images/Banner/Main." + fileName.Split(".")[1];              
                    break;
                }
            case Purpose.OrganizerAboutMeImage:
                {           
                    key = $"public/{organizerId}/Profile/AboutMe." + fileName.Split(".")[1];              
                    break;
                }
             case Purpose.OrganizerDocument:
                {           
                    key = $"public/{organizerId}/Profile/AboutMe." + fileName.Split(".")[1];              
                    break;
                }
             default:
                break;
           
        }
        return key;
    }
    public async Task UploadFileAsync(int organizerId, int  eventId,  Stream fileStream, string fileName, string contentType, Purpose purpose)
    {
        if (organizerId <=0 || fileStream == null || string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("Customer id, event id, file stream, and file name must be provided.");
        }


        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = GetFileKey(fileName, organizerId, purpose, eventId),
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);
    }
   

    public async Task<string> GetPreSignedUrlForUpload(string fileName, int organizerId, Purpose purpose, string contentType,int eventId=0)
    {
        if (string.IsNullOrEmpty(fileName) || organizerId<=0)
        {
            throw new ArgumentException("File name and customer id must be provided");
        }
        //todo verify content type
        // Generate a pre-signed URL for the file upload
        var preSignedUrl = await _s3Client.GetPreSignedURLAsync(new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = GetFileKey(fileName, organizerId,purpose, eventId),
            Verb = Amazon.S3.HttpVerb.PUT,
            ContentType= contentType,
            Expires = DateTime.UtcNow.AddMinutes(_maxTimeForUrl) // URL valid for 15 minutes
        });

        // Implementation for generating a pre-signed URL
        // This is a placeholder for the actual URL generation logic
        Console.WriteLine($"Generating pre-signed URL for {fileName} in bucket {BucketName}");
        return preSignedUrl;
    }

    public static string ConvertKeyToUrl(string key)
    {
        return $"https://{BucketName}.s3.us-west-2.amazonaws.com/{key}";
    }
}