using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.DnsProviders
{
    public class AzureDnsProvider : IDnsProvider
    {
        private readonly IDnsManagementClient client;
        private readonly AzureDnsSettings settings;

        public AzureDnsProvider(AzureDnsSettings settings)
        {
            var credentials = AzureHelper.GetAzureCredentials(settings.AzureServicePrincipal, settings.AzureSubscription);
         
            this.client = new DnsManagementClient(credentials);
            this.client.SubscriptionId = settings.AzureSubscription.SubscriptionId;
            this.settings = settings;
        }

        public int MinimumTtl => 60;

        public async Task Cleanup(string recordSetName)
        {
            var existingRecords = await SafeGetExistingRecords(recordSetName);

            await this.client.RecordSets.DeleteAsync(this.settings.ResourceGroupName, this.settings.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT);
        }

        public async Task PersistChallenge(string recordSetName, string recordValue)
        {
            List<TxtRecord> records = new List<TxtRecord>()
            {
                new TxtRecord() { Value = new[] { recordValue } }
            };
            if ((await client.RecordSets.ListByTypeAsync(settings.ResourceGroupName, settings.ZoneName, RecordType.TXT)).Any())
            {
                var existingRecords = await SafeGetExistingRecords(recordSetName);
                if (existingRecords != null)
                {
                    if (existingRecords.TxtRecords.Any(s => s.Value.Contains(recordValue)))
                    {
                        records = existingRecords.TxtRecords.ToList();
                    }
                    else
                    {
                        records.AddRange(existingRecords.TxtRecords);
                    }
                }
            }
            await this.client.RecordSets.CreateOrUpdateAsync(this.settings.ResourceGroupName, this.settings.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT, new RecordSetInner()
            {
                TxtRecords = records,
                TTL = MinimumTtl
            });
        }

        private string GetRelativeRecordSetName(string dnsTxt)
        {
            return dnsTxt.Replace($".{this.settings.ZoneName}", "");
        }

        private async Task<RecordSetInner> SafeGetExistingRecords(string recordSetName)
        {
            try
            {
                return await client.RecordSets.GetAsync(settings.ResourceGroupName, settings.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT);

            }
            catch (CloudException cex)
            {
                if (!cex.Message.StartsWith("The resource record "))
                {
                    throw;
                }
            }
            return null;
        }
    }
}
