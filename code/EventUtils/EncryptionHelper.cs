namespace EventUtils;

using System;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;


public class EncryptionHelper
{
    private static string Key {get; set; } = string.Empty;

    public EncryptionHelper(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
        Key = configuration["Encryption:Key"] ?? throw new ArgumentException("Encryption key is not configured.", nameof(configuration));
        if (string.IsNullOrEmpty(Key))
            throw new ArgumentException("Encryption key is not configured.", nameof(configuration));
    }


    public static string Encrypt(string plainText,bool urlEncode = false)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV and encrypted bytes
        var result = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
        
        return !urlEncode? Convert.ToBase64String(result): UrlEncode(Convert.ToBase64String(result));
    }

    public static string UrlEncode(string input)
    {
        return input
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');
    }
    public static string UrlDecode(string base64Url) {
        string base64 = base64Url.Replace("-", "+").Replace("_", "/");
        // Add padding back if needed for Convert.FromBase64String
        switch (base64.Length % 4) {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return base64;
    }
   

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));

        // Extract IV
        var iv = new byte[aes.BlockSize / 8];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

}