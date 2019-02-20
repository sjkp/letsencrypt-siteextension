using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Services
{
    /// <summary>
    /// Class to help resolve the acme-challange folder in azure web apps.
    /// </summary>
    public class PathProvider
    {
        private readonly IAzureWebAppEnvironment azureEnvironment;
        private bool virtualDirectorySetup = false;
        private readonly string wellKnownPhysicalPath = @"site\letsencrypt\.well-known";

        public PathProvider(IAzureWebAppEnvironment azureEnvironment)
        {
            this.azureEnvironment = azureEnvironment;
        }
        public async Task<string> WebRootPath(bool uriPath)
        {
            var path = string.Empty;
            if (!string.IsNullOrEmpty(azureEnvironment.WebRootPath))
            {
                //User supplied webroot path, just use it
                path = azureEnvironment.WebRootPath;
            }
            else if (this.azureEnvironment.RunFromPackage)
            {
                if (bool.TryParse(ConfigurationManager.AppSettings[AppSettingsAuthConfig.disableVirtualApplication], out var disable) && disable)
                {
                    Trace.TraceInformation($"Disabling usage of virtual applications '{AppSettingsAuthConfig.disableVirtualApplication}:{disable}'");
                }
                else
                {
                    Trace.TraceInformation($"Setting up virtual application at /.well-known");
                    await EnsureVirtualDirectorySetup();
                }
                path = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "letsencrypt");
            }
            else
            {
                path = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot");
            }
            return uriPath ? MakeUriPath(path) : path;
        }        

        private async Task EnsureVirtualDirectorySetup()
        {
            if (virtualDirectorySetup)
            {
                Trace.TraceInformation("/.well-known already configured, skipping");
                return;
            }
            using (var client = await ArmHelper.GetWebSiteManagementClient(this.azureEnvironment))
            {
                var siteConfig = client.WebApps.GetSiteConfigurationOrSlot(azureEnvironment.ResourceGroupName, azureEnvironment.WebAppName, azureEnvironment.SiteSlotName);

                if (IsVirtualDirectorySetup(siteConfig))
                {
                    siteConfig.VirtualApplications.First().VirtualDirectories.Add(new VirtualDirectory()
                    {
                        PhysicalPath = wellKnownPhysicalPath,
                        VirtualPath = "/.well-known",
                    });
                    client.WebApps.UpdateSiteConfigurationOrSlot(azureEnvironment.ResourceGroupName, azureEnvironment.WebAppName, azureEnvironment.SiteSlotName, siteConfig);
                }
                virtualDirectorySetup = true;
            }
        }

        public async Task<bool> IsVirtualDirectorySetup()
        {
            using (var client = await ArmHelper.GetWebSiteManagementClient(this.azureEnvironment))
            {
                var siteConfig = client.WebApps.GetSiteConfigurationOrSlot(azureEnvironment.ResourceGroupName, azureEnvironment.WebAppName, azureEnvironment.SiteSlotName);

                return IsVirtualDirectorySetup(siteConfig);               
            }
        }

        private bool IsVirtualDirectorySetup(SiteConfigResource siteConfig)
        {
            var isSetupAsAppliction = siteConfig.VirtualApplications.Any(s => s.PhysicalPath.StartsWith(wellKnownPhysicalPath));
            var isSetupAsDirectory = siteConfig.VirtualApplications.Any(s => s.VirtualDirectories.Any(d => d.PhysicalPath.StartsWith(wellKnownPhysicalPath)));

            return isSetupAsAppliction || isSetupAsDirectory;
        }

        public async Task<string> ChallengeDirectory(bool uriPath)
        {
            
                var webRootPath = await WebRootPath(uriPath);
                var directory = Path.Combine(webRootPath, ".well-known", "acme-challenge");
                return directory;
            
        }

        private static string MakeUriPath(string s)
        {
            return s.Replace(Environment.ExpandEnvironmentVariables("%HOME%"), "").Replace('\\', '/');
        }


    }
}
