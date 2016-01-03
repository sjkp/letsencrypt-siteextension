using LetsEncrypt.SiteExtension.Models;
using System;

namespace LetsEncrypt.SiteExtension
{
    public class Target : IAuthSettings
    {
        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        public string Host { get; internal set; }

        public string Email { get; set; }
        public string BaseUri { get; internal set; }

        public Guid ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string WebAppName { get; internal set; }
        public string ResourceGroupName { get; internal set; }
        public Guid SubscriptionId { get; internal set; }
        public string Tenant { get; internal set; }      
    }
}