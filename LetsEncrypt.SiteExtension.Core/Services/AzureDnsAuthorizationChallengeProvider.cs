using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;

namespace LetsEncrypt.Azure.Core.Services
{
    public class AzureDnsAuthorizationChallengeProvider : BaseDnsAuthorizationChallengeProvider
    {
        private readonly DnsManagementClient dnsClient;
        private IAzureDnsEnvironment environment;

        public AzureDnsAuthorizationChallengeProvider(IAzureDnsEnvironment azureDnsSettings) 
        {
            this.dnsClient = ArmHelper.GetDnsManagementClient(azureDnsSettings);
            this.environment = azureDnsSettings;
        }

        public override async Task CleanupChallenge(DnsChallenge dnsChallenge)
        {
            var existingRecords = await dnsClient.RecordSets.GetAsync(environment.ResourceGroupName, environment.ZoneName, environment.RelativeRecordSetName, RecordType.TXT);
            
            await this.dnsClient.RecordSets.CreateOrUpdateAsync(this.environment.ResourceGroupName, this.environment.ZoneName, environment.RelativeRecordSetName, RecordType.TXT, new RecordSet()
            {
                TxtRecords = existingRecords.TxtRecords.Where(s => !s.Value.Contains(dnsChallenge.RecordValue)).ToList(),
                TTL = 60
            });
        }

        public override async Task PersistsChallenge(DnsChallenge dnsChallenge)
        {
            List<TxtRecord> records = new List<TxtRecord>()
            {
                new TxtRecord() { Value = new[] { dnsChallenge.RecordValue } }
            };
            if ((await dnsClient.RecordSets.ListByTypeAsync(environment.ResourceGroupName, environment.ZoneName, RecordType.TXT)).Any())
            {
                var existingRecords = await dnsClient.RecordSets.GetAsync(environment.ResourceGroupName, environment.ZoneName, environment.RelativeRecordSetName, RecordType.TXT);
                if (existingRecords.TxtRecords.Any(s => s.Value.Contains(dnsChallenge.RecordValue)))
                {
                    records = existingRecords.TxtRecords.ToList();
                }
                else
                {
                    records.AddRange(existingRecords.TxtRecords);
                }
            }
            await this.dnsClient.RecordSets.CreateOrUpdateAsync(this.environment.ResourceGroupName, this.environment.ZoneName, environment.RelativeRecordSetName, RecordType.TXT, new RecordSet()
            {
                TxtRecords = records,
                TTL = 60
            });
        }
    }
}
