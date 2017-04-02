using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;

namespace LetsEncrypt.SiteExtension.WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {            
            var config = new JobHostConfiguration();
            config.UseTimers();
            //A host ID must be between 1 and 32 characters, contain only lowercase letters, numbers, and 
            //dashes, not start or end with a dash, and not contain consecutive dashes.
            var hostId = "letsencrypt-" + Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");            
            config.HostId = hostId.Substring(0,hostId.Length > 32 ? 32 : hostId.Length).TrimEnd(new[] { '-' }).ToLower();

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
