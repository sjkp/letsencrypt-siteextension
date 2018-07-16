using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LetsEncrypt.Azure.Core.Models
{
    /// <summary>
    /// Information about the requested certificate.
    /// </summary>
    public class CertificateInfo
    {
        [JsonIgnore]
        public X509Certificate2 Certificate { get; set; }
        /// <summary>
        /// The name of the certificate.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Password of the certificate.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The byte content of the pfx certificate.
        /// </summary>
        public byte[] PfxCertificate { get; set; }
    }
}
