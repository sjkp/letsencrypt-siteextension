using ACMESharp;
using Certes;
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
        /// <summary>
        /// Returns the authorization status from lets encrypt. 
        /// </summary>
        /// <param name="dnsIdentifiers"></param>
        /// <returns></returns>
        Task<string> Authorize(List<string> dnsIdentifiers);
        void RegisterClient(object client);
    }
}
