using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Models
{
    /// <summary>
    /// Result of the certificate installation.
    /// </summary>
    public class CertificateInstallModel : ICertificateInstallModel
    {
        /// <summary>
        /// List of DNS names the certificate was requested for.
        /// </summary>
        public List<string> AllDnsIdentifiers
        {
            get; set;
        }

        /// <summary>
        /// Certificate info.
        /// </summary>
        public CertificateInfo CertificateInfo
        {
            get; set;
        }

        /// <summary>
        /// The primary host name. 
        /// </summary>
        public string Host
        {
            get; set;
        }
    }

    /// <summary>
    /// Settings for whether to use Server Name Indication (SNI) or IP-based SSL bindings.
    /// </summary>
    public class CertificateServiceSettings : IWebAppCertificateSettings
    {
        /// <summary>
        /// If set to true IP-based SSL will be used.
        /// </summary>
        public bool UseIPBasedSSL
        {
            get; set;
        }
    }

    public interface ICertificateInstallModel
    {
        CertificateInfo CertificateInfo { get; set; }


        List<string> AllDnsIdentifiers { get; set; }
        
        string Host { get; set; }
    }

    public interface IWebAppCertificateSettings
    {
        bool UseIPBasedSSL { get; set; }
    }
}
