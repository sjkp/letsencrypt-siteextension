using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace ARMExplorer.Controllers
{
    public static class Utils
    {
        public const string X_MS_OAUTH_TOKEN = "X-MS-OAUTH-TOKEN";
        public const string X_MS_Ellapsed = "X-MS-Ellapsed";
        public const string AntaresApiVersion = "2014-06-01";
        public const string CSMApiVersion = "2014-04-01";

        public const string resourcesTemplate = "{0}/subscriptions/{1}/resources?api-version={2}";
        public const string subscriptionTemplate = "{0}/subscriptions/{1}?api-version={2}";

        public static string GetApiVersion(string path)
        {
            if (path.IndexOf("/Microsoft.Web/", StringComparison.OrdinalIgnoreCase) > 0)
            {
                return AntaresApiVersion;
            }

            return CSMApiVersion;
        }

        public static async Task<HttpResponseMessage> Execute(Task<HttpResponseMessage> task)
        {
            var watch = new Stopwatch();
            watch.Start();
            var response = await task;
            watch.Stop();
            response.Headers.Add(Utils.X_MS_Ellapsed, watch.ElapsedMilliseconds + "ms");
            return response;
        }


        public static string GetCSMUrl(string host)
        {
            if (host.EndsWith(".antares-int.windows-int.net", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api-next.resources.windows-int.net";
            }
            else if (host.EndsWith(".antares-test.windows-int.net", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api-current.resources.windows-int.net";
            }
            else if (host.EndsWith(".ant-intapp.windows-int.net", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api-dogfood.resources.windows-int.net";
            }
            else if (host.EndsWith(".waws-ppedf.windows-int.net", StringComparison.OrdinalIgnoreCase))
            {
                return "https://api-dogfood.resources.windows-int.net";
            }

            return "https://management.azure.com";
        }
    }
}