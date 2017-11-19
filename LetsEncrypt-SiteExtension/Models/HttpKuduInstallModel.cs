using LetsEncrypt.Azure.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.SiteExtension.Models
{
    public class HttpKuduInstallModel
    {
        [Required]
        public AzureWebAppEnvironment AzureEnvironment { get; set; }

        [Required]
        public AcmeConfig AcmeConfig { get; set; }

        [Required]
        public CertificateServiceSettings CertificateSettings { get; set; }

        [Required]
        public AuthorizationChallengeProviderConfig AuthorizationChallengeProviderConfig { get; set; }
    }
}