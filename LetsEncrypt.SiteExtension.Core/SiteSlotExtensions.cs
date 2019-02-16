using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LetsEncrypt.Azure.Core
{
    public static class SiteSlotExtensions
    {
        public static Site GetSiteOrSlot(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.Get(resourceGroupName, webAppName);
            }
            else
            {
                return sites.GetSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }

        public static SiteConfigResource GetSiteConfigurationOrSlot(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.GetConfiguration(resourceGroupName, webAppName);
            }
            else
            {
                return sites.GetConfigurationSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }

        public static SiteConfigResource UpdateSiteConfigurationOrSlot(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName, SiteConfigResource config)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.UpdateConfiguration(resourceGroupName, webAppName, config);
            }
            else
            {
                return sites.UpdateConfigurationSlot(resourceGroupName, webAppName, config, siteSlotName);
            }
        }



        public static StringDictionary ListSiteOrSlotAppSettings(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.ListApplicationSettings(resourceGroupName, webAppName);
            }
            else
            {
                return sites.ListApplicationSettingsSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }
        public static StringDictionary UpdateSiteOrSlotAppSettings(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName, StringDictionary settings)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.UpdateApplicationSettings(resourceGroupName, webAppName, settings);
            }
            else
            {
                //We want the slot settings to be fixed to the slot, so we don't swap the wrong LetsEncrypt webjob settings into production.
                var existingSlotConfigs = sites.ListSlotConfigurationNames(resourceGroupName, webAppName);
                if (existingSlotConfigs.AppSettingNames == null)
                    existingSlotConfigs.AppSettingNames = new List<string>();
                var updateRequired = false;
                foreach(var appSettingName in settings.Properties.Keys)
                {
                    if (!existingSlotConfigs.AppSettingNames.Contains(appSettingName))
                    {
                        existingSlotConfigs.AppSettingNames.Add(appSettingName);
                        updateRequired = true;
                    }
                }
                var res = sites.UpdateApplicationSettingsSlot(resourceGroupName, webAppName, settings, siteSlotName);
                if (updateRequired)
                {
                    sites.UpdateSlotConfigurationNames(resourceGroupName, webAppName, existingSlotConfigs);
                }

                return res;
            }
        }
        public static HostNameBinding CreateOrUpdateSiteOrSlotHostNameBinding(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName, string hostName, HostNameBinding hostNameBinding)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.CreateOrUpdateHostNameBinding(resourceGroupName, webAppName, hostName, hostNameBinding);
            }
            else
            {
                return sites.CreateOrUpdateHostNameBindingSlot(resourceGroupName, webAppName, hostName, hostNameBinding, siteSlotName);
            }
        }
        public static Site BeginCreateOrUpdateSiteOrSlot(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName, Site s)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.BeginCreateOrUpdate(resourceGroupName, webAppName, s);
            }
            else
            {
                return sites.BeginCreateOrUpdateSlot(resourceGroupName, webAppName, s, siteSlotName);
            }
        }

        public static User GetPublsihingCredentialSiteOrSlot(this IWebAppsOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {

            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.BeginListPublishingCredentials(resourceGroupName, webAppName);
            }
            else
            {
                return sites.BeginListPublishingCredentialsSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }
    }
}