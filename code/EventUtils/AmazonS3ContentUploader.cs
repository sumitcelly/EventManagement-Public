
using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace EventUtils;

public class AmazonS3ContentUploader
{
   
    private readonly int _maxTimeForUrl = 3; // in minutes

    private readonly AmazonS3Client _s3Client;
    public AmazonS3ContentUploader(IConfiguration configuration)
    {
        _s3Client = new AmazonS3Client(
            configuration["AccessKeyId"],
            configuration["AccessKeySecret"],
            Amazon.RegionEndpoint.USWest2);
        BucketName = configuration["S3BucketName"] ?? "customereventcontent";
        if (string.IsNullOrEmpty(BucketName))
        {
            throw new ArgumentException("S3 bucket name is not configured.");
        }
       
    }
    
    public static string BucketName { get; set; } = "customereventcontent";
    
    public static string GetFileKey(string fileName, string customerName, string eventName)
    {
        // Example key format: "customer event content/CustomerName/Events/EventName/"
        string key = string.Empty;
        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            key = GetBannerImageKey(customerName, eventName) + fileName;
        }
        else if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
        {
            key = GetDocumentKey(customerName, eventName) + fileName;
        }
        else
        {
            throw new ArgumentException("Unsupported file type. Only .jpg, .png, .pdf, and .doc files are allowed.");
        }
        return key;
    }

    public static string GetBannerImageKey(string customerName, string eventName)
    {
        // Example key format: "customer event content/CustomerName/Events/EventName/Images/Banner/"
        return @$"{customerName}/Events/{eventName}/Images/Banner/";
    }
   
   public static string GetDocumentKey(string customerName, string eventName)
    {
        // Example key format: "customer event content/CustomerName/Events/EventName/"
        return @$"{customerName}/Events/{eventName}/Documents/";
    }

    public async Task UploadFileAsync(string customerName, string eventName,  Stream fileStream, string fileName, string contentType)
    {
        if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(eventName) || fileStream == null || string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("Customer name, event name, file stream, and file name must be provided.");
        }


        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = GetFileKey(fileName, customerName, eventName),
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);
    }
   

    public async Task<string> GetPreSignedUrlForUpload(string fileName, string customerName, string eventName)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentException("File name, customer name, and event name must be provided.");
        }
        
        // Generate a pre-signed URL for the file upload
        var preSignedUrl = await _s3Client.GetPreSignedURLAsync(new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = GetFileKey(fileName, customerName, eventName),
            Verb = Amazon.S3.HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_maxTimeForUrl) // URL valid for 15 minutes
        });

        // Implementation for generating a pre-signed URL
        // This is a placeholder for the actual URL generation logic
        Console.WriteLine($"Generating pre-signed URL for {fileName} in bucket {BucketName}");
        return preSignedUrl;
    }
    
}