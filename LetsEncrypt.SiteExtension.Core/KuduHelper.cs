using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.WebSites;
using System;

namespace LetsEncrypt.Azure.Core
{
    public static class KuduHelper
    {
        public static KuduRestClient GetKuduClient(this WebSiteManagementClient client, IAzureWebAppEnvironment settings)
        {
            var user = client.WebApps.GetPublsihingCredentialSiteOrSlot(settings.ResourceGroupName, settings.WebAppName, settings.TipSlotName);
            var site = client.WebApps.GetSiteOrSlot(settings.ResourceGroupName, settings.WebAppName, settings.TipSlotName);
            var defaultHostName = site.DefaultHostName;

            return new KuduRestClient(MakeScmUri(defaultHostName,settings), user.PublishingUserName, user.PublishingPassword);
        }

        /// <summary>
        /// TODO; should also work for APP service environment, which uses a different format for scm site uri https://blogs.msdn.microsoft.com/benjaminperkins/2017/11/08/how-to-access-kudu-scm-for-an-azure-app-service-environment-ase/
        /// </summary>
        /// <param name="defaultHostName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Uri MakeScmUri(string defaultHostName, IAzureWebAppEnvironment settings)
        {
            var i = defaultHostName.IndexOf("." + settings.AzureWebSitesDefaultDomainName);
            return new Uri($"https://{defaultHostName.Insert(i, ".scm")}");
        }
    }
}
