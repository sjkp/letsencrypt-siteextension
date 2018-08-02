using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    /// <summary>
    /// Result of the certificate installation.
    /// </summary>
    public class CertificateInstallModel : ICertificateInstallModel
    {
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

    public interface ICertificateInstallModel
    {
        CertificateInfo CertificateInfo { get; set; }

        string Host { get; set; }
    }    
}
