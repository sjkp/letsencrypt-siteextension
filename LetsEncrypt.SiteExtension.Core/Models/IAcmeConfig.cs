using System.Collections.Generic;

namespace LetsEncrypt.Azure.Core.Models
{
    public interface IAcmeConfig
    {
        string RegistrationEmail { get; }

        string BaseUri { get;  }

        /// <summary>
        /// The host name the certificate should be issued for.
        /// </summary>
        string Host { get;  }

        IEnumerable<string> Hostnames
        {
            get;
        }

        List<string> AlternateNames { get;  }

        int RSAKeyLength { get;  }

        string PFXPassword { get; }

        /// <summary>
        /// Should the Lets Encrypt production environment be used. 
        /// Only checked if <see cref="BaseUri"/> isn't set. 
        /// </summary>
        bool UseProduction { get; }
    }
}