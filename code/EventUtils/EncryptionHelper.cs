namespace EventUtils;

using System;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;


public class EncryptionHelper
{
   


    public static string Encrypt(string plainText,string secretKey)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("Key cannot be null or empty.", nameof(secretKey));
  
           // Ensure key is exactly 32 bytes (256 bits)
        byte[] key = Encoding.UTF8.GetBytes(secretKey.PadRight(32).Substring(0, 32));
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);

        // AES-GCM requires a 12-byte Nonce (instead of a 16-byte IV)
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        // Prepare buffers for ciphertext and the authentication tag (16 bytes)
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[16];

        using (var aesGcm = new AesGcm(key, tagSizeInBytes: 16))
        {
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        // Combine Nonce + Tag + Ciphertext into one payload
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        // Convert to URL-safe Base64 string
        return Convert.ToBase64String(result).Replace("+", "-").Replace("/", "_").TrimEnd('=');    
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
   

    public static string Decrypt(string cipherTextUrlSafe,string secretKey)
    {
        if (string.IsNullOrEmpty(cipherTextUrlSafe))
            throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherTextUrlSafe));
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("Key cannot be null or empty.", nameof(secretKey));

        byte[] key = Encoding.UTF8.GetBytes(secretKey.PadRight(32).Substring(0, 32));
    
        // Restore standard Base64 formatting
        string base64 = cipherTextUrlSafe.Replace("-", "+").Replace("_", "/");
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        byte[] encryptedPayload = Convert.FromBase64String(base64);

        // Extract the components
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        byte[] ciphertext = new byte[encryptedPayload.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(encryptedPayload, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedPayload, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(encryptedPayload, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        byte[] plaintextBytes = new byte[ciphertext.Length];

        using (var aesGcm = new AesGcm(key, tagSizeInBytes: 16))
        {
            // This will automatically throw an exception if the data was modified
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        }

        return Encoding.UTF8.GetString(plaintextBytes);

    }

}