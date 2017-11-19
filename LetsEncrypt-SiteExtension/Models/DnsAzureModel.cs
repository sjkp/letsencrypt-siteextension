using LetsEncrypt.Azure.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class DnsAzureModel 
    {
        [Required]
        public AzureDnsEnvironment AzureDnsEnvironment { get; set; }

        [Required]
        public AcmeConfig AcmeConfig { get; set; }
    }
}