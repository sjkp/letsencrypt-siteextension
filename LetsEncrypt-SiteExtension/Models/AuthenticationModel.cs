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
    }
}