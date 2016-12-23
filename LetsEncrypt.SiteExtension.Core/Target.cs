using LetsEncrypt.SiteExtension.Models;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.SiteExtension
{
    public class Target : IAuthSettings
    {
        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        public string Host { get; set; }

        public string Email { get; set; }
        public string BaseUri { get; set; }

        public Guid ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string WebAppName { get; set; }
        public string SiteSlotName { get; set; }
        public string ResourceGroupName { get; set; }

        public string ServicePlanResourceGroupName { get; set; }

        public Guid SubscriptionId { get; set; }
        public string Tenant { get; set; }
        public List<string> AlternativeNames { get; set; }
        
        public List<string> AllDnsIdentifiers
        {
            get
            {
                List<string> allDnsIdentifiers = new List<string>();
                allDnsIdentifiers.Add(this.Host);

                if (this.AlternativeNames != null)
                {
                    allDnsIdentifiers.AddRange(this.AlternativeNames);
                }
                return allDnsIdentifiers;
            }
        }

        public bool UseIPBasedSSL { get; set; }
    }
}