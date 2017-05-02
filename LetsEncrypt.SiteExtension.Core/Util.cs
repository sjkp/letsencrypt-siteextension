using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LetsEncrypt.Azure.Core
{
    public static class Util
    {
        public static bool IsAzure
        {
            get
            {
                // Rely on an environment variable to determine if we're on Azure
                return Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null;
            }
        }
    }
}