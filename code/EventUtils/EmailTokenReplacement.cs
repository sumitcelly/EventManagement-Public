using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Configuration;

namespace EventUtils
{
    public class TokenValues
    {
        public string QRCode { get; set; } = string.Empty;

        public string QRCodeImage { get; set; } = string.Empty;

        public string EventName { get; set; } = string.Empty;

        public string EventLocalDate { get; set; }= string.Empty;

        public string EventLocalTime { get; set; } = string.Empty;

        public string Attendee { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public string EventOrganizerName { get; set; } = string.Empty;

        public string EventOrganizerEmail { get; set; } = string.Empty;

        public string EventTicketLink { get; set; } = string.Empty;

        public string EmailCode {get; set;} = string.Empty;
        public string GrandTotal { get;  set; } = string.Empty;
        public string? VenueName { get;  set; }
    }
    /// <summary>
    /// Handles the replacement of tokens in email templates
    /// </summary>
    public class EmailTokenReplacement
    {
        private readonly Dictionary<string, string> _templateTokenMap;

        private static readonly Dictionary<string,string> _globalTokens=new Dictionary<string, string>();
        
        public static readonly List<string> _supportedTokens = new List<string>
        {
            "venue_address",
            "venue_name",
            "event_name",
            "event_date",
            "event_time",
            "attendee_name",
            "organizer_email",
            "organizer_name",
            "ticket_url",
            //email verification
            "email_code",
             //order tokens
            "order_code",
            "grand_total",
           
            //global or footer tokens
            "support_email",
            "privacy_url",
            "registered_company",
            "registered_address",
            "platform_name"
        };

     
        public EmailTokenReplacement(IConfiguration config)
        {
            _templateTokenMap = new Dictionary<string, string>();
            foreach (string token in _supportedTokens)
            {
                _templateTokenMap[token] = $"{{{{{token}}}}}";
            }

            _globalTokens.Add("{{support_email}}",  config["EmailTemplateValues:support_email"]??string.Empty);
            _globalTokens.Add("{{privacy_url}}",  config["EmailTemplateValues:privacy_url"]??string.Empty);
            _globalTokens.Add("{{registered_company}}",  config["EmailTemplateValues:registered_company"]??string.Empty);
            _globalTokens.Add("{{registered_address}}",  config["EmailTemplateValues:registered_address"]??string.Empty);
            _globalTokens.Add("{{platform_name}}",  config["EmailTemplateValues:platform_name"]??string.Empty);
        }

        public static Dictionary<string, string> GetReplacementValues(TokenValues tokenValues)
        {
            var values = new Dictionary<string, string>();
            foreach (var token in EmailTokenReplacement._supportedTokens)
            {
                switch (token)
                {
                    case "order_code":
                        values[token] = tokenValues.QRCode;
                        break;
                    case "event_name":
                        values[token] = tokenValues.EventName;
                        break;
                    //not used
                    case "attendee_name":
                        values[token] = tokenValues.Attendee ?? string.Empty;
                        break;
                    case "event_date":
                        values[token] = tokenValues.EventLocalDate;
                        break;
                    case "event_time":
                        values[token] = tokenValues.EventLocalTime;
                        break;
                    case "venue_address":
                        values[token] = tokenValues.EventLocation ?? string.Empty;
                        break;
                    case "venue_name":
                        values[token] = tokenValues.VenueName ?? string.Empty;
                        break;
                    case "organizer_name":
                        values[token] = tokenValues.EventOrganizerName ?? "Not specified";
                        break;
                    case "grand_total":
                           values[token] = tokenValues.GrandTotal;
                           break;
                    case "organizer_email":
                        values[token] = tokenValues.EventOrganizerEmail ?? "Not specified";
                        break;
                    case "ticket_url":
                        values[token] = tokenValues.EventTicketLink ?? string.Empty;
                        break;
                    case "email_code":
                        values[token] = tokenValues.EmailCode ?? string.Empty;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        public string ReplacePlatformNameInSubject(string subject)
        {
            if (string.IsNullOrEmpty(subject))
            {
                return subject;
            }
            else
            {
                return subject.Replace("{{platform_name}}",_globalTokens["{{platform_name}}"]);
            }
        }

        public string ReplaceEventNameInSubject(string subject,string eventName)
        {
            if (string.IsNullOrEmpty(subject))
            {
                return subject;
            }
            else
            {
                return subject.Replace("{{event_name}}",eventName);
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
                if (replacementValues.TryGetValue(token.Key, out var value) && !string.IsNullOrEmpty(value))
                {
                    template = template.Replace(token.Value, value);
                }
            }
            foreach(var globalToken in _globalTokens)
            {
                template = template.Replace(globalToken.Key, globalToken.Value);
            }

            return template;
        }
    }
        
}