using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureDnsProviderService : IDnsProviderService
    {
        private readonly IDnsManagementClient client;
        private readonly IAzureDnsEnvironment environment;

        public AzureDnsProviderService(IDnsManagementClient client, IAzureDnsEnvironment environment)
        {
            this.client = client;
            this.environment = environment;
        }
        public async Task Cleanup(string recordSetName)
        {
            var existingRecords = await SafeGetExistingRecords(recordSetName);

            await this.client.RecordSets.DeleteAsync(this.environment.ResourceGroupName, this.environment.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT);
        }

        public async Task PersistChallenge(string recordSetName, string recordValue)
        {
            List<TxtRecord> records = new List<TxtRecord>()
            {
                new TxtRecord() { Value = new[] { recordValue } }
            };
            if ((await client.RecordSets.ListByTypeAsync(environment.ResourceGroupName, environment.ZoneName, RecordType.TXT)).Any())
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
            await this.client.RecordSets.CreateOrUpdateAsync(this.environment.ResourceGroupName, this.environment.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT, new RecordSetInner()
            {
                TxtRecords = records,
                TTL = 60
            });
        }

        private string GetRelativeRecordSetName(string dnsTxt)
        {
            return dnsTxt.Replace($".{this.environment.ZoneName}", "");
        }

        private async Task<RecordSetInner> SafeGetExistingRecords(string recordSetName)
        {
            try
            {
                return await client.RecordSets.GetAsync(environment.ResourceGroupName, environment.ZoneName, GetRelativeRecordSetName(recordSetName), RecordType.TXT);

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
