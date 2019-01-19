using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure;

namespace LetsEncrypt.Azure.Core.Services
{
    public class AzureDnsAuthorizationChallengeProvider : BaseDnsAuthorizationChallengeProvider
    {
        private IAzureDnsEnvironment environment;

        public AzureDnsAuthorizationChallengeProvider(IAzureDnsEnvironment azureDnsSettings) 
        {            
            this.environment = azureDnsSettings;
        }

        public override async Task CleanupChallenge(DnsChallenge dnsChallenge)
        {
            var existingRecords = await SafeGetExistingRecords(dnsChallenge);
            var dnsClient = await ArmHelper.GetDnsManagementClient(this.environment);
            await dnsClient.RecordSets.DeleteAsync(this.environment.ResourceGroupName, this.environment.ZoneName, GetRelativeRecordSetName(dnsChallenge), RecordType.TXT);
        }

        public override async Task PersistsChallenge(DnsChallenge dnsChallenge)
        {
            List<TxtRecord> records = new List<TxtRecord>()
            {
                new TxtRecord() { Value = new[] { dnsChallenge.RecordValue } }
            };
            var dnsClient = await ArmHelper.GetDnsManagementClient(this.environment);
            if ((await dnsClient.RecordSets.ListByTypeAsync(environment.ResourceGroupName, environment.ZoneName, RecordType.TXT)).Any())
            {
                var existingRecords = await SafeGetExistingRecords(dnsChallenge);
                if (existingRecords != null)
                {
                    if (existingRecords.TxtRecords.Any(s => s.Value.Contains(dnsChallenge.RecordValue)))
                    {
                        records = existingRecords.TxtRecords.ToList();
                    }
                    else
                    {
                        records.AddRange(existingRecords.TxtRecords);
                    }
                }
            }
            await dnsClient.RecordSets.CreateOrUpdateAsync(this.environment.ResourceGroupName, this.environment.ZoneName, GetRelativeRecordSetName(dnsChallenge), RecordType.TXT, new RecordSet()
            {
                TxtRecords = records,
                TTL = 60
            });
        }

        private string GetRelativeRecordSetName(DnsChallenge dnsChallenge)
        {
            return dnsChallenge.RecordName.Replace($".{this.environment.ZoneName}", "");
        }

        private async Task<RecordSet> SafeGetExistingRecords(DnsChallenge dnsChallenge)
        {
            try
            {
                var dnsClient = await ArmHelper.GetDnsManagementClient(this.environment);
                return await dnsClient.RecordSets.GetAsync(environment.ResourceGroupName, environment.ZoneName, GetRelativeRecordSetName(dnsChallenge), RecordType.TXT);

            }
            catch (CloudException cex)
            {
                if (!cex.Message.StartsWith("The resource record '_acme-challenge"))
                {
                    throw;
                }
            }
            return null;
        }
    }
}
