using System;
using System.Collections.Generic;

namespace EventUtils
{
    public class TokenValues
    {
        public string QRCode { get; set; } = string.Empty;

        public string QRCodeImage { get; set; } = string.Empty;

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Attendee { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public string EventOrganizerName { get; set; } = string.Empty;

        public string EventOrganizerHelpLine { get; set; } = string.Empty;

        public string EventTicketLink { get; set; } = string.Empty;
    }
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
            "EventOrganizerHelpLine",
            "EventTicketLink"
        };

        public EmailTokenReplacement()
        {
            _templateTokenMap = new Dictionary<string, string>();
            foreach (var token in _supportedTokens)
            {
                _templateTokenMap[token] = $"[token_{token}]";
            }
        }

        public static Dictionary<string, string> GetReplacementValues(TokenValues tokenValues)
        {
            var values = new Dictionary<string, string>();
             foreach (var token in EmailTokenReplacement._supportedTokens)
            {
                switch (token)
                {
                    case "QRCode":
                        values[token] = tokenValues.QRCode;
                        break;
                    case "QRCodeImage":
                        values[token] = tokenValues.QRCodeImage;
                        break;
                    case "EventName":
                        values[token] = tokenValues.EventName;
                        break;
                    case "Attendee":
                        values[token] = tokenValues.Attendee ?? string.Empty;
                        break;
                    case "EventDate":
                        values[token] = tokenValues.EventDate.ToString("yyyy-MM-dd");
                        break;
                    case "EventLocation":
                        values[token] = tokenValues.EventLocation ?? "Not specified";
                        break;
                    case "EventOrganizerName":
                        values[token] = tokenValues.EventOrganizerName ?? "Not specified";
                        break;
                    case "EventOrganizerHelpLine":
                        values[token] = tokenValues.EventOrganizerHelpLine ?? "Not specified";
                        break;
                    case "EventTicketLink":
                        values[token] = tokenValues.EventTicketLink ?? string.Empty;
                        break;
                    default:
                        break;
                }
            }
            return values;
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