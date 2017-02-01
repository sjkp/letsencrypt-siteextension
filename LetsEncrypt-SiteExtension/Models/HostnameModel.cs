using System.Collections.Generic;
using Microsoft.Azure.Management.WebSites.Models;

namespace LetsEncrypt.SiteExtension.Models
{
    public class HostnameModel
    {
        public HostnameModel()
        {
            Certificates = new List<Certificate>();
            HostNames = new List<string>();
            HostNameSslStates = new List<HostNameSslState>();
        }

        public IList<Certificate> Certificates { get; internal set; }

        public bool Error
        {
            get
            {
                return !string.IsNullOrEmpty(ErrorMessage);
            }
        }

        public string ErrorMessage { get; internal set; }
        public IList<string> HostNames { get; set; }
        public IList<HostNameSslState> HostNameSslStates { get; set; }
        public string InstalledCertificateThumbprint { get; internal set; }
    }
}