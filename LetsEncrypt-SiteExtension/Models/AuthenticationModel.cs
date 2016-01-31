using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class AuthenticationModel : IAuthSettings
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

        [Display(Name = "Update Application Settings")]
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
                ServicePlanResourceGroupName = config.ServicePlanResourceGroupName,                
            };
        }
    }
}