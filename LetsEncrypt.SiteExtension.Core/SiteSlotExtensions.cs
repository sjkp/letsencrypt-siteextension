using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension
{
    public static class SiteSlotExtensions
    {
        public static Site GetSiteOrSlot(this ISitesOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.GetSite(resourceGroupName, webAppName);
            }
            else
            {
                return sites.GetSiteSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }
        public static StringDictionary ListSiteOrSlotAppSettings(this ISitesOperations sites, string resourceGroupName, string webAppName, string siteSlotName)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.ListSiteAppSettings(resourceGroupName, webAppName);
            }
            else
            {
                return sites.ListSiteAppSettingsSlot(resourceGroupName, webAppName, siteSlotName);
            }
        }
        public static StringDictionary UpdateSiteOrSlotAppSettings(this ISitesOperations sites, string resourceGroupName, string webAppName, string siteSlotName, StringDictionary settings)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.UpdateSiteAppSettings(resourceGroupName, webAppName, settings);
            }
            else
            {
                //We want the slot settings to be fixed to the slot, so we don't swap the wrong LetsEncrypt webjob settings into production.
                var existingSlotConfigs = sites.GetSlotConfigNames(resourceGroupName, webAppName);
                var updateRequired = false;
                foreach(var appSettingName in settings.Properties.Keys)
                {
                    if (!existingSlotConfigs.AppSettingNames.Contains(appSettingName))
                    {
                        existingSlotConfigs.AppSettingNames.Add(appSettingName);
                        updateRequired = true;
                    }
                }
                var res = sites.UpdateSiteAppSettingsSlot(resourceGroupName, webAppName, settings, siteSlotName);
                if (updateRequired)
                {
                    sites.UpdateSlotConfigNames(resourceGroupName, webAppName, existingSlotConfigs);
                }

                return res;
            }
        }
        public static HostNameBinding CreateOrUpdateSiteOrSlotHostNameBinding(this ISitesOperations sites, string resourceGroupName, string webAppName, string siteSlotName, string hostName, HostNameBinding hostNameBinding)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.CreateOrUpdateSiteHostNameBinding(resourceGroupName, webAppName, hostName, hostNameBinding);
            }
            else
            {
                return sites.CreateOrUpdateSiteHostNameBindingSlot(resourceGroupName, webAppName, hostName, hostNameBinding, siteSlotName);
            }
        }
        public static Site BeginCreateOrUpdateSiteOrSlot(this ISitesOperations sites, string resourceGroupName, string webAppName, string siteSlotName, Site s)
        {
            if (string.IsNullOrEmpty(siteSlotName))
            {
                return sites.BeginCreateOrUpdateSite(resourceGroupName, webAppName, s);
            }
            else
            {
                return sites.BeginCreateOrUpdateSiteSlot(resourceGroupName, webAppName, s, siteSlotName);
            }
        }
    }
}