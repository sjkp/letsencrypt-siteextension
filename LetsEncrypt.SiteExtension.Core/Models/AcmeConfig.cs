using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Models
{
    public class AcmeConfig
    {
        public string RegistrationEmail {get;set;}

        public string Endpoint { get; set; }

        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        public string Host { get; set; }

        public List<string> AllDnsIdentifiers
        {
            get
            {
                List<string> allDnsIdentifiers = new List<string>();
                allDnsIdentifiers.Add(this.Host);

                if (this.AlternativeNames != null)
                {
                    allDnsIdentifiers.AddRange(this.AlternativeNames);
                }
                return allDnsIdentifiers;
            }
        }

        public List<string> AlternativeNames { get; set; }

        public int RSAKeyLength { get; set; }

        public string PFXPassword { get; set; }
    }
}
