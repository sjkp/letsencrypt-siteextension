using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Models
{
    public class CertificateInstallModel : ICertificateInstallModel
    {
        public List<string> AllDnsIdentifiers
        {
            get; set;
        }

        public CertificateInfo CertificateInfo
        {
            get; set;
        }

        public string Host
        {
            get; set;
        }
    }

    public class CertificateServiceSettings : ICertificateServiceSettings
    {
        public bool UseIPBasedSSL
        {
            get;set;
        }
    }

    public interface ICertificateInstallModel
    {
        CertificateInfo CertificateInfo { get; set; }


        List<string> AllDnsIdentifiers { get; set; }
        
        string Host { get; set; }
    }

    public interface ICertificateServiceSettings
    {
        bool UseIPBasedSSL { get; set; }
    }
}
