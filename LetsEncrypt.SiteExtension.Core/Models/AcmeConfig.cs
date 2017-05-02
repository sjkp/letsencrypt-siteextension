using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Models
{
    public class AcmeConfig : IAcmeConfig
    {
        public string RegistrationEmail { get; set; }

        public string BaseUri { get; set; }

        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        public string Host { get; set; }

        public IEnumerable<string> Hostnames
        {
            get
            {
                List<string> allDnsIdentifiers = new List<string>();
                allDnsIdentifiers.Add(this.Host);

                if (this.AlternateNames != null)
                {
                    allDnsIdentifiers.AddRange(this.AlternateNames);
                }
                return allDnsIdentifiers;
            }
        }

        public List<string> AlternateNames { get; set; }

        public int RSAKeyLength { get; set; }

        public string PFXPassword { get; set; }
    }
}
