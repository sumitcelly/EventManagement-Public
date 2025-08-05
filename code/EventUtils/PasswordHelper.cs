using System;
using System.Text;
using System.Security.Cryptography;

public class PasswordHelper
{
    // Generates a salted hash using SHA256
    public static string HashPassword(string password, out string salt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        // Generate a random salt
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        salt = Convert.ToBase64String(saltBytes);

        // Combine password and salt
        var combined = Encoding.UTF8.GetBytes(password + salt);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }

    // Verifies a password against a hash and salt
    public static bool VerifyPassword(string password, string salt, string hash)
    {
        var combined = Encoding.UTF8.GetBytes(password + salt);
        using var sha256 = SHA256.Create();
        var computedHash = sha256.ComputeHash(combined);
        var computedHashString = Convert.ToBase64String(computedHash);
        return computedHashString == hash;
    }
}