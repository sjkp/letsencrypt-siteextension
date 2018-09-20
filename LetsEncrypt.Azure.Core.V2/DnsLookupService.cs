using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class DnsLookupService
    {
        private readonly ILogger<DnsLookupService> logger;

        public DnsLookupService(ILogger<DnsLookupService> logger = null)
        {
            this.logger = logger ?? NullLogger<DnsLookupService>.Instance;
        }

        public async Task<bool> Exists(string hostname, string dnsTxt, int timeout = 60)
        {
            logger.LogInformation("Starting dns precheck validation for hostname: {HostName} challenge: {Challenge} and timeout {Timeout}", hostname, dnsTxt, timeout);
            var idn = new IdnMapping();
            hostname = idn.GetAscii(GetNoneWildcardDomain(hostname));
            var dnsClient = GetDnsClient(hostname);
            var startTime = DateTime.UtcNow;
            var valid = false;
            //string queriedDns = "";
            //Lets encrypt checks a random authoritative server, thus we need to ensure that all respond with the challenge. 
            foreach (var ns in dnsClient.NameServers)
            {
                logger.LogInformation("Validating dns challenge exists on name server {NameServer}", ns.ToString());
                do
                {
                    var dnsRes = dnsClient.QueryServer(new[] { ns.Endpoint.Address }, $"_acme-challenge.{hostname}", QueryType.TXT);
                    var queriedDns = dnsRes.Answers.TxtRecords();
                   
                    if (!queriedDns.Any(_ => _.Text.Any(t => t == dnsTxt)))
                    {
                        logger.LogInformation("Challenge record was {existingTxt} should have been {Challenge}, retrying again in 5 seconds", queriedDns, dnsTxt);
                        await Task.Delay(5000);
                    }
                    else
                        valid = true;

                } while (!valid && (DateTime.UtcNow - startTime).TotalSeconds < timeout);
            }

            return valid;
        }

        private static LookupClient GetDnsClient(params string[] hostnames)
        {

            LookupClient generalClient = new LookupClient();
            LookupClient dnsClient = null;
            generalClient.UseCache = false;
            foreach (var hostname in hostnames)
            {
                var ns = generalClient.Query(hostname, QueryType.NS);
                var ip = ns.Answers.NsRecords().Select(s => generalClient.GetHostEntry(s.NSDName.Value));
            
                dnsClient = new LookupClient(ip.SelectMany(i => i.AddressList).Where(s => s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray());
                dnsClient.UseCache = false;
                
            }

            return dnsClient;
        }

        public static string GetNoneWildcardDomain(string hostname)
        {
            var parts = DnsString.Parse(hostname);
            return $"{parts.Labels[2]}{parts.Labels[1]}";
        }
    }
}
