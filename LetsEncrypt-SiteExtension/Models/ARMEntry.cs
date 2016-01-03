using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    // An Azure Resource Manager object with standard envelope fields
    public class ARMEntry<T> where T: INamedObject
    {
        public static ARMEntry<T> Create(T o, HttpRequestMessage request, bool isChild = false)
        {
            var armEntry = new ARMEntry<T>() { Properties = o };

            // In Azure ARM requests, the referrer is the current id
            Uri referrer = request.Headers.Referrer;
            if (referrer != null)
            {
                armEntry.Id = referrer.AbsolutePath;
            }
            else
            {
                armEntry.Id = request.RequestUri.AbsolutePath;
            }

            // If we're generating a child object, append the child name
            if (isChild)
            {
                if (!armEntry.Id.EndsWith("/")) armEntry.Id+='/';

                armEntry.Id += o.Name;
            }

            // The Type and Name properties use alternating token starting with 'Microsoft.Web/sites'
            // e.g. /subscriptions/b0019e1d-2829-4226-9356-4a57a4a5cc90/resourcegroups/MyRG/providers/Microsoft.Web/sites/MySite/extensions/SettingsAPISample/settings/foo1
            // Type: Microsoft.Web/sites/extensions/settings
            // Name: MySite/SettingsAPISample/foo1

            string[] idTokens = armEntry.Id.Split('/');
            if (idTokens.Length > 8 && idTokens[6] == "Microsoft.Web")
            {
                armEntry.Type = idTokens[6];

                for (int i = 7; i < idTokens.Length; i += 2)
                {
                    armEntry.Type += "/" + idTokens[i];
                }

                armEntry.Name = String.Empty;
                for (int i = 8; i < idTokens.Length; i += 2)
                {
                    armEntry.Name += "/" + idTokens[i];
                }
            }

            IEnumerable<string> values;
            if (request.Headers.TryGetValues("x-ms-geo-location", out values))
            {
                armEntry.Location = values.FirstOrDefault();
            }

            return armEntry;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public T Properties { get; set; }
    }
}