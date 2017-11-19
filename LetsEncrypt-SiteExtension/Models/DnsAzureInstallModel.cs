using System;
using LetsEncrypt.Azure.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.SiteExtension.Models
{
    public class DnsAzureInstallModel : IAzureDnsEnvironment
    {
        [Required]
        public AzureWebAppEnvironment AzureWebAppEnvironment { get; set; }

        [Required]
        public AcmeConfig AcmeConfig { get; set; }

        [Required]
        public CertificateServiceSettings CertificateSettings { get; set; }

        /// <summary>
        /// The relative record set name.
        /// </summary>
        [Required]
        public string RelativeRecordSetName
        {
            get;set;
        }

        /// <summary>
        /// The zone name. 
        /// </summary>
        [Required]
        public string ZoneName
        {
            get;set;
        }
        string resourceGroupName;

        /// <summary>
        /// The resource group name defaults to AzureWebAppEnvironment.ResourceGroupName.
        /// </summary>
        public string ResourceGroupName
        {
            get
            {
                return resourceGroupName ?? AzureWebAppEnvironment?.ResourceGroupName;
            }
            set
            {
                resourceGroupName = value;
            }
        }

        string tenant;
        /// <summary>
        /// Tenant defaults to AzureWebAppEnvironment.Tenant.
        /// </summary>
        public string Tenant
        {
            get
            {
                return tenant ?? AzureWebAppEnvironment?.Tenant;
            }
            set
            {
                tenant = value;
            }
        }

        Guid clientId;
        /// <summary>
        /// The client id defaults to AzureWebAppEnvironment.ClientId.
        /// </summary>
        public Guid ClientId
        {
            get
            {
                return clientId == Guid.Empty ? AzureWebAppEnvironment.ClientId : clientId;
            }
            set
            {
                clientId = value;
            }
        }

        string clientSecret;
        /// <summary>
        /// The client secret defaults to AzureWebAppEnvironment.ClientSecret.
        /// </summary>
        public string ClientSecret
        {
            get
            {
                return clientSecret ?? AzureWebAppEnvironment?.ClientSecret;
            }
            set
            {
                clientSecret = value;
            }
        }

        Guid subscriptionId;

        /// <summary>
        /// The subscription id defaults to AzureWebAppEnvironment.SubscriptionId.
        /// </summary>
        public Guid SubscriptionId
        {
            get
            {
                return subscriptionId == Guid.Empty ? AzureWebAppEnvironment.SubscriptionId : subscriptionId;
            }
            set
            {
                subscriptionId = value;
            }
        }

        Uri managementEndpoint;

        /// <summary>
        /// The management endpoint defaults to AzureWebAppEnvironment.ManagementEndpoint.
        /// </summary>
        public Uri ManagementEndpoint
        {
            get
            {
                return managementEndpoint ?? AzureWebAppEnvironment.ManagementEndpoint;
            }
            set
            {
                managementEndpoint = value;
            }
        }

        Uri tokenAudience;

        /// <summary>
        /// The token audience defaults to AzureWebAppEnvironment.TokenAudience.
        /// </summary>
        public Uri TokenAudience
        {
           get
            {
                return tokenAudience ?? AzureWebAppEnvironment.TokenAudience;
            }
            set
            {
                tokenAudience = value;
            }
        }

        Uri authenticationEndpoint;
        /// <summary>
        /// The authentication endpoint to sign in to. Defaults to AzureWebAppEnvironment.AuthenticationEndpoint.
        /// </summary>
        public Uri AuthenticationEndpoint
        {
            get
            {
                return authenticationEndpoint ?? AzureWebAppEnvironment.AuthenticationEndpoint;
            }
            set
            {
                authenticationEndpoint = value;
            }
        }
    }
}