using LetsEncrypt.Azure.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class AuthenticationModel : IAzureWebAppEnvironment
    {
        [Required]
        public string Tenant { get; set; }

        [Required]
        public Guid SubscriptionId
        {
            get; set;
        }

        [Required]
        public Guid ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        [Required]
        public string ResourceGroupName
        {
            get; set;
        }

        [Required]
        public string WebAppName
        {
            get; set;
        }
        public string TipSlotName
        {
            get; set;
        }
        public string SiteSlotName
        {
            get; set;
        }

        [Required]
        public bool UseIPBasedSSL
        {
            get; set;
        }

        [Required]
        public string DashboardConnectionString
        {
            get; set;
        }

        [Required]
        public string StorageConnectionString
        {
            get; set;
        }

        [Display(Name = "Update Application Settings and Virtual Directory (if needed)")]
        public bool UpdateAppSettings
        {
            get;set;
        }

        public string ErrorMessage
        {
            get; set;
        }

        public bool Error
        {
            get
            {
                return !string.IsNullOrEmpty(ErrorMessage);
            }
        }
        
        public string ServicePlanResourceGroupName { get; set; }

        public string AzureWebSitesDefaultDomainName
        {
            get; set;
        }

        public Uri ManagementEndpoint
        {
            get; set;
        }

        public Uri TokenAudience
        {
            get; set;
        }

        public Uri AuthenticationEndpoint
        {
            get;set;
        }

        public string WebRootPath
        {
            get; set;
        }

        public bool RunFromPackage { get; set; }

        public static explicit operator AuthenticationModel(AppSettingsAuthConfig config)
        {
            return new AuthenticationModel()
            {
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                ResourceGroupName = config.ResourceGroupName,
                SubscriptionId = config.SubscriptionId,
                Tenant = config.Tenant,
                WebAppName = config.WebAppName,
                UseIPBasedSSL = config.UseIPBasedSSL,
                ServicePlanResourceGroupName = config.ServicePlanResourceGroupName,                
                AuthenticationEndpoint = config.AuthenticationEndpoint,
                AzureWebSitesDefaultDomainName = config.AzureWebSitesDefaultDomainName,
                ManagementEndpoint = config.ManagementEndpoint,
                TokenAudience = config.TokenAudience,
                TipSlotName = config.TipSlotName,
                SiteSlotName = config.SiteSlotName,
                WebRootPath = config.WebRootPath,
                RunFromPackage = config.RunFromPackage,
                DashboardConnectionString = config.DashboardConnectionString,
                StorageConnectionString = config.StorageConnectionString,
            };
        }
    }
}