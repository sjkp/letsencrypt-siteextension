using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureServicePrincipal 
    {
        public bool UseManagendIdentity { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public byte[] Certificate { get; set; }
        public string CertificatePassword { get; set; }

        internal ServicePrincipalLoginInformation ServicePrincipalLoginInformation => new ServicePrincipalLoginInformation()
        {
            Certificate = this.Certificate,
            ClientId = this.ClientId,
            ClientSecret = this.ClientSecret,
            CertificatePassword = this.CertificatePassword
        };
    }
}
