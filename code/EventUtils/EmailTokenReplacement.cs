using System;
using System.Collections.Generic;

namespace EventUtils
{
    /// <summary>
    /// Handles the replacement of tokens in email templates
    /// </summary>
    public class EmailTokenReplacement
    {
        private readonly Dictionary<string, string> _templateTokenMap;
        public static readonly List<string> _supportedTokens = new List<string>
        {
            "QRCode",
            "QRCodeImage",
            "EventName",
            "EventDate",
            "Attendee",
            "EventLocation",
            "EventOrganizerName",
            "EventOrganizerHelpLine"

        };

        public EmailTokenReplacement()
        {
            _templateTokenMap = new Dictionary<string, string>();
            foreach (var token in _supportedTokens)
            {
                _templateTokenMap[token] = $"[token_{token}]";
            }
        }

        /// <summary>
        /// Replaces tokens in the provided template with their corresponding values
        /// </summary>
        /// <param name="template">The email template containing tokens</param>
        /// <returns>The processed template with replaced tokens</returns>
        public string ReplaceTokens(string template, Dictionary<string, string> replacementValues)
        {
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (replacementValues == null || replacementValues.Count == 0)
            {
                throw new ArgumentNullException(nameof(template));
            }

            foreach (var token in _templateTokenMap)
            {
                if (replacementValues.TryGetValue(token.Key, out var value))
                {
                    template = template.Replace(token.Value, value);
                }
            }

            return template;
        }
    }
        
}