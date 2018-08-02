using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using LetsEncrypt.Azure.Core.V2;
using Microsoft.Extensions.Configuration;
using LetsEncrypt.Azure.Core.V2.Models;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.CertificateStores;

namespace LetsEncrypt.Azure.Runner
{
    class Program
    {
        static IConfiguration Configuration;
        async static Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                  .AddJsonFile("settings.json", true)
                  .AddEnvironmentVariables()
                  .Build();

            var azureAppSettings = new AzureWebAppSettings[] { };
            
            if (Configuration.GetSection("AzureAppService").Exists())
            {
                azureAppSettings = new[] { Configuration.GetSection("AzureAppService").Get<AzureWebAppSettings>() };
            }
            if (Configuration.GetSection("AzureAppServices").Exists())
            {
                azureAppSettings = Configuration.GetSection("AzureAppServices").Get<AzureWebAppSettings[]>();
            }

            if (azureAppSettings.Length == 0)
            {
                throw new ArgumentNullException("Must provide either AzureAppService configuration section or AzureAppServices configuration section");
            }

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(c =>
            {
                c.AddConsole();
                //c.AddDebug();
            })
            .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)            
            .AddAzureAppService(azureAppSettings);

            string azureStorageConnectionString = Configuration.GetConnectionString("AzureStorageAccount");
            if (Configuration.GetSection("DnsSettings").Get<GoDaddyDnsProvider.GoDaddyDnsSettings>().ShopperId != null)
            {
                serviceCollection.AddAcmeClient<GoDaddyDnsProvider>(Configuration.GetSection("DnsSettings").Get<GoDaddyDnsProvider.GoDaddyDnsSettings>(), azureStorageConnectionString);
            } else if (Configuration.GetSection("DnsSettings").Get<UnoEuroDnsSettings>().AccountName != null)
            {
                serviceCollection.AddAcmeClient<UnoEuroDnsProvider>(Configuration.GetSection("DnsSettings").Get<UnoEuroDnsSettings>(), azureStorageConnectionString);
            } else if (Configuration.GetSection("DnsSettings").Get<AzureDnsSettings>().ResourceGroupName != null)
            {
                serviceCollection.AddAcmeClient<AzureDnsProvider>(Configuration.GetSection("DnsSettings").Get<AzureDnsSettings>(), azureStorageConnectionString);
            }

            serviceCollection.AddTransient<App>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var dnsRequest = Configuration.GetSection("AcmeDnsRequest").Get<AcmeDnsRequest>();

            var app = serviceProvider.GetService<App>();
            await app.Run(dnsRequest, Configuration.GetValue<int?>("RenewXNumberOfDaysBeforeExpiration") ?? 22);
        }
    }
}
