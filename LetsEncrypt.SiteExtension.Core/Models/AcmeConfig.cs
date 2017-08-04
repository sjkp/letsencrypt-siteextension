using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Models
{
    public class AcmeConfig : IAcmeConfig
    {
        [Required]
        public string RegistrationEmail { get; set; }

        public string BaseUri { get; set; }

        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        [Required]
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

        [Required]
        [Range(1024,8096)]
        public int RSAKeyLength { get; set; }

        public string PFXPassword { get; set; }

        /// <summary>
        /// Should the Lets Encrypt production environment be used. 
        /// Only checked if <see cref="BaseUri"/> isn't set. 
        /// </summary>
        public bool UseProduction
        {
            get; set;
        }
    }
}
