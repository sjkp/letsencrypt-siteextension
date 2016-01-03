using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class RequestAndInstallModel
    {
        [Required]
        public string Hostname { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool UseStaging { get; set; }
    }
}