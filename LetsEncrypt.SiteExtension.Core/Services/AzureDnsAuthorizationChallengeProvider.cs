using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.Dns;

namespace LetsEncrypt.Azure.Core.Services
{
    public class AzureDnsAuthorizationChallengeProvider : BaseDnsAuthorizationChallengeProvider
    {
        private readonly DnsManagementClient dnsClient;
        private IAzureEnvironment environment;

        public AzureDnsAuthorizationChallengeProvider(IAzureEnvironment azureEnvironment) 
        {
            this.dnsClient = ArmHelper.GetDnsManagementClient(azureEnvironment);
            this.environment = azureEnvironment;
        }

        public override Task CleanupChallenge(DnsChallenge httpChallenge)
        {
            
        }

        public override Task PersistsChallenge(DnsChallenge httpChallenge)
        {
            this.dnsClient.RecordSets.CreateOrUpdate(this.environment.ResourceGroupName,)
        }
    }
}
