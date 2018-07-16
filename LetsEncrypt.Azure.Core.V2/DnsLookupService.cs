using DnsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class DnsLookupService
    {
        public async Task<bool> Exists(string hostname, string dnsTxt, int timeout = 60)
        {
            var dnsClient = GetDnsClient(hostname);
            var startTime = DateTime.UtcNow;
            string queriedDns = "";
            do
            {
                var dnsRes = dnsClient.Query($"_acme-challenge.{GetNoneWildcardDomain(hostname)}", QueryType.TXT);
                queriedDns = dnsRes.Answers.TxtRecords().FirstOrDefault()?.Text.FirstOrDefault();
                if (queriedDns != dnsTxt)
                    await Task.Delay(5000);                
            } while (queriedDns != dnsTxt && (DateTime.UtcNow - startTime).TotalSeconds < timeout);

            return queriedDns == dnsTxt;
        }

        private static LookupClient GetDnsClient(params string[] hostnames)
        {

            LookupClient generalClient = new LookupClient();
            LookupClient dnsClient = null;
            generalClient.UseCache = false;
            foreach (var hostname in hostnames)
            {
                var ns = generalClient.Query(GetNoneWildcardDomain(hostname), QueryType.NS);
                var ip = generalClient.GetHostEntry(ns.Answers.NsRecords().Select(s => s.NSDName.Value).First());
                if (dnsClient != null && !ip.AddressList.SequenceEqual(dnsClient.NameServers.Select(s => s.Endpoint.Address)))
                    throw new Exception("domain names are on different nameservers");
                dnsClient = new LookupClient(ip.AddressList);
                dnsClient.UseCache = false;
            }

            return dnsClient;
        }

        public static string GetNoneWildcardDomain(string hostname)
        {
            return hostname.Replace("*.", "");
        }
    }
}
