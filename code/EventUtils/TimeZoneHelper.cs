using GeoTimeZone;
using TimeZoneConverter;

using System;

namespace EventUtils
{
    public static class TimeZoneConverter
    {
        
            public  static  (string LocalDate, string LocalTime) GetLocalDateTime(double lat, double lng, DateTime utcDateTime)
            {
                // 1. Get the IANA Time Zone ID from coordinates
                // Returns something like "Europe/London" or "America/New_York"
                var timezoneResult = TimeZoneLookup.GetTimeZone(lat, lng);
                string ianaId = timezoneResult.Result;

                // 2. Convert IANA ID to a .NET TimeZoneInfo object
                // This library ensures it works on both Windows and Linux servers
                TimeZoneInfo tzInfo = TZConvert.GetTimeZoneInfo(ianaId);

                // 3. Convert the UTC time to the Venue's Local Time
                DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tzInfo);

                // 4. Format for your MJML tokens
                // "D" is Long Date (e.g., Monday, June 15, 2024)
                // "t" is Short Time (e.g., 7:00 PM)
                return (
                    localDateTime.ToString("D"), 
                    localDateTime.ToString("t")
                );
}

    }
}