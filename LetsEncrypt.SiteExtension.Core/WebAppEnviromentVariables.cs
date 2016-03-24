using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class WebAppEnviromentVariables
    {
        private Guid subscriptionId;
        private string resourceGroupName;

        public WebAppEnviromentVariables()
        {
            ParserWebSiteOwner();
        }

        private void ParserWebSiteOwner()
        {
            var websiteowner = ConfigurationManager.AppSettings["WEBSITE_OWNER_NAME"];
            if (string.IsNullOrEmpty(websiteowner))
            {
                Trace.TraceInformation("App Setting WEBSITE_OWNER_NAME is null or empty");
                return;
            }
            try {
                
                //format: 688bf064-900b-4e8f-9598-2d9be0718133+Tiimo.Web.Dev1-WestEuropewebspace

                Guid.TryParse(websiteowner.Split('+').FirstOrDefault(), out subscriptionId);
                var arr = string.Join("+", websiteowner.Split('+').Skip(1)).Split('-');
                resourceGroupName = string.Join("-", arr.Take(arr.Length - 1));
            } catch(Exception ex)
            {
                Trace.TraceError(string.Format("unable to parse WEBSITE_OWNER_NAME '{0}' exception '{1}'", websiteowner, ex.ToString()));
            }
        }

        public string ResourceGroupName
        {
            get
            {
                return resourceGroupName;
            }
        }

        public Guid SubscriptionId
        {
            get
            {
                return subscriptionId;
            }
        }
    }
}