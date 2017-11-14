using Newtonsoft.Json;
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
        /// <summary>
        /// The registration email used when registering the Let's Encrypt certificate.
        /// Will receive notification when certificates expire. 
        /// </summary>
        [Required]
        public string RegistrationEmail { get; set; }

        /// <summary>
        /// The Let's Encrypt API url. 
        /// </summary>
        public string BaseUri { get; set; }

        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        [Required]
        public string Host { get; set; }

        [JsonIgnore]
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

        /// <summary>
        /// Alternate DNS names besides the host name that should be included in the certificate.
        /// </summary>
        public List<string> AlternateNames { get; set; }

        /// <summary>
        /// The Certificate key length. Defaults to 2048.
        /// </summary>
        [Required]
        [Range(1024, 8096)]
        public int RSAKeyLength { get; set; } = 2048;

        /// <summary>
        /// The password used to protect the pfx file. 
        /// </summary>
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
