using ACMESharp;
using LetsEncrypt.Azure.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Services
{
    public interface IAuthorizationChallengeProvider
    {
        Task<AuthorizationState> Authorize(AcmeClient client, List<string> dnsIdentifiers);
    }
}
