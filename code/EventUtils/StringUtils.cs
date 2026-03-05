
using System.Text.RegularExpressions;

namespace EventUtils;

public static class StringUtils
{
    public static string CreateUrlSlug(string inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return string.Empty;

        // 1. Remove special characters (keep alphanumeric and spaces)
        string cleanedString = Regex.Replace(inputString, @"[^a-zA-Z0-9\s]", "");

        // 2. Remove all spaces (matching your JS logic)
        string slug = Regex.Replace(cleanedString, @"\s+", "");

        // 3. Convert to lowercase
        return slug.ToLowerInvariant();
    }

}